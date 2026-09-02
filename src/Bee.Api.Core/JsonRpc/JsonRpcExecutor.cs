using System.Diagnostics;
using System.Reflection;
using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Base.Tracing;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Api.Core.Validator;
using Bee.Api.Core.Conversion;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// JSON-RPC request executor.
    /// </summary>
    public class JsonRpcExecutor
    {
        private static readonly char[] s_methodSeparators = new[] { '.' };

        private readonly IBusinessObjectFactory _boFactory;
        private readonly IAccessTokenValidator _tokenValidator;
        private readonly IApiEncryptionKeyProvider _keyProvider;
        private readonly IAnomalyLogWriter? _anomalyWriter;
        private readonly AuditLogOptions? _auditOptions;
        private readonly ISessionInfoService? _sessionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcExecutor"/> class.
        /// </summary>
        /// <param name="boFactory">The business-object factory.</param>
        /// <param name="tokenValidator">The access-token validator.</param>
        /// <param name="keyProvider">The API encryption key provider.</param>
        /// <param name="anomalyWriter">
        /// Optional audit writer for API anomaly records; null disables API anomaly logging.
        /// Supplied by DI; direct construction (e.g. tests) may omit it.
        /// </param>
        /// <param name="auditOptions">Optional audit-log options (anomaly enable + API slow threshold).</param>
        /// <param name="sessionService">Optional session lookup for the acting user (denormalised who).</param>
        public JsonRpcExecutor(
            IBusinessObjectFactory boFactory,
            IAccessTokenValidator tokenValidator,
            IApiEncryptionKeyProvider keyProvider,
            IAnomalyLogWriter? anomalyWriter = null,
            AuditLogOptions? auditOptions = null,
            ISessionInfoService? sessionService = null)
        {
            _boFactory = boFactory ?? throw new ArgumentNullException(nameof(boFactory));
            _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
            _anomalyWriter = anomalyWriter;
            _auditOptions = auditOptions;
            _sessionService = sessionService;
        }

        /// <summary>
        /// Gets or sets the access token used to identify the current user or session.
        /// </summary>
        public Guid AccessToken { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the call originates from a local source (e.g., the same process or host as the server).
        /// </summary>
        public bool IsLocalCall { get; set; } = false;

        /// <summary>
        /// Gets or sets the API key verdict for the current call, assigned by the transport layer.
        /// Defaults to <see cref="ApiKeyValidationResult.NotChecked"/>, which is correct for
        /// in-process calls: they carry no <c>X-Api-Key</c> header.
        /// </summary>
        public ApiKeyValidationResult ApiKeyValidation { get; set; } = ApiKeyValidationResult.NotChecked;

        /// <summary>
        /// Executes an API method.
        /// </summary>
        /// <remarks>
        /// This blocks on the asynchronous path. Every <c>await</c> it reaches uses
        /// <c>ConfigureAwait(false)</c>, so it does not deadlock on a host with a
        /// <see cref="System.Threading.SynchronizationContext"/> — but a business object that
        /// resumes on the captured context would reintroduce that. Prefer
        /// <see cref="ExecuteAsync"/> from any asynchronous caller.
        /// </remarks>
        /// <param name="request">The JSON-RPC request model.</param>
        public JsonRpcResponse Execute(JsonRpcRequest request)
        {
            return ExecuteAsyncCore(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        public Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
        {
            return ExecuteAsyncCore(request);
        }

        /// <summary>
        /// Internal asynchronous execution core logic.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        private async Task<JsonRpcResponse> ExecuteAsyncCore(JsonRpcRequest request)
        {
            var ctx = Tracer.Start(TraceLayers.ApiServer, string.Empty, name: request.Method);
            var response = new JsonRpcResponse(request);
            var stopwatch = AnomalyEnabled ? Stopwatch.StartNew() : null;
            try
            {
                var format = request.Params.Format;

                // Parse method, create BO, and validate access BEFORE decryption.
                // This ensures unauthenticated or unauthorized requests are rejected without
                // performing any decryption work.
                var (progId, action) = ParseMethod(request.Method);
                var businessObject = CreateBusinessObject(AccessToken, progId);
                // Hand the caller's application identity to business objects that ask for it. A
                // setter rather than a constructor argument: only a few methods care (the
                // connectivity probe reports it, the audit trail records it), and widening every
                // business-object constructor would break each application subclass for their sake.
                if (businessObject is IApiKeyContextAware apiKeyAware)
                {
                    apiKeyAware.ApiKeyValidation = ApiKeyValidation;
                }
                var method = GetMethod(businessObject, action);
                ApiAccessValidator.ValidateAccess(method, new ApiCallContext(AccessToken, IsLocalCall, format), _tokenValidator);

                // Access confirmed: retrieve the encryption key and decrypt the payload.
                byte[]? apiEncryptionKey = GetApiEncryptionKey(format);
                // The frame rides inside the envelope, so the replay gate can only run once the
                // payload is decrypted — it is a second gate after ValidateAccess, not part of it.
                ApiPayloadConverter.RestoreFrom(request.Params, format, apiEncryptionKey);
                ValidateFrameTimestamp(request.Params.Frame);
                ValidateFrameSequence(method, request.Params.Frame);

                // Invoke the method and convert the result.
                var value = await InvokeMethodAsync(businessObject, method, request.Params.Value)
                    .ConfigureAwait(false);
                value = ApiOutputConverter.Convert(value!);

                // The answer is written with the codec the caller asked for, the same way it keeps
                // the caller's format. A client that negotiated one cannot decode anything else.
                response.Result = new JsonRpcResult { Value = value, Codec = request.Params.Codec };
                ApiPayloadConverter.TransformTo(response.Result, format, apiEncryptionKey);
                Tracer.End(ctx);
                LogApiSlowAnomaly(request.Method, stopwatch);
            }
            catch (Exception ex)
            {
                var rootEx = ex.Unwrap();
                // Map the exception to a (code, message) pair. User-facing exceptions surface
                // their original message; infrastructure exceptions are flattened to a generic
                // message to avoid leaking internals.
                var (code, message) = MapException(rootEx);
                response.Error = new JsonRpcError((int)code, message);
                Tracer.End(ctx, TraceStatus.Error, rootEx.Message);
                LogApiFailureAnomaly(request.Method, rootEx, stopwatch);
            }
            return response;
        }

        /// <summary>
        /// Refuses a call whose frame timestamp is too far from server time.
        /// </summary>
        /// <param name="frame">The frame read from the request, or null when none was required.</param>
        /// <exception cref="ReplayRejectedException">Thrown when the drift exceeds the configured tolerance.</exception>
        private static void ValidateFrameTimestamp(ApiPayloadFrame? frame)
        {
            if (frame == null) { return; }

            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long driftMs = Math.Abs(nowMs - frame.TimestampMs);
            double toleranceMs = ApiServiceOptions.WireFrameTimestampTolerance.TotalMilliseconds;

            if (driftMs > toleranceMs)
            {
                throw new ReplayRejectedException(
                    $"The request timestamp is {driftMs / 1000} seconds away from server time, outside the accepted window. Check the client clock.");
            }
        }

        /// <summary>
        /// Refuses a call whose sequence number this session has already used.
        /// </summary>
        /// <param name="method">The method being invoked, whose declaration says whether to check.</param>
        /// <param name="frame">The frame read from the request, or null when none was required.</param>
        /// <exception cref="ReplayRejectedException">Thrown when the sequence repeats or is out of range.</exception>
        /// <remarks>
        /// Skipped for anonymous callers: sequence numbers are counted per session, and a call made
        /// without one has nothing to count against — every anonymous caller would otherwise share
        /// a single window and evict each other's numbers.
        /// </remarks>
        private void ValidateFrameSequence(MethodInfo method, ApiPayloadFrame? frame)
        {
            if (frame == null || AccessToken == Guid.Empty) { return; }

            var attr = ApiAccessValidator.FindAccessControl(method);
            if (attr?.ReplayProtection != ApiReplayProtection.UniqueSequence) { return; }

            var window = ApiServiceOptions.ReplayWindowStore.GetOrAdd(AccessToken);
            if (!window.TryAccept(frame.Sequence))
            {
                throw new ReplayRejectedException(
                    "This request repeats a sequence number the session has already used, or falls outside the accepted range.");
            }
        }

        #region Anomaly detection

        private bool AnomalyEnabled =>
            _anomalyWriter != null && _sessionService != null
            && _auditOptions is { Enabled: true, AnomalyEnabled: true };

        /// <summary>Records a Slow anomaly when a completed call exceeds the configured threshold.</summary>
        private void LogApiSlowAnomaly(string method, Stopwatch? stopwatch)
        {
            if (stopwatch == null || _auditOptions == null) { return; }
            stopwatch.Stop();
            int threshold = _auditOptions.ApiSlowThresholdMs;
            if (threshold > 0 && stopwatch.ElapsedMilliseconds > threshold)
                WriteApiAnomaly(method, AnomalyKind.Slow, stopwatch.ElapsedMilliseconds, thresholdMs: threshold);
        }

        /// <summary>Records an Error / Timeout anomaly for a failed call.</summary>
        private void LogApiFailureAnomaly(string method, Exception rootEx, Stopwatch? stopwatch)
        {
            if (stopwatch == null) { return; }
            stopwatch.Stop();
            // A replay rejection is filed under its own kind: unlike an Error it says nothing is
            // broken, and a run of them points at a drifted client clock or a caller resending
            // captured packets — neither of which is visible once folded into generic errors.
            var kind = rootEx is ReplayRejectedException ? AnomalyKind.Replay
                : IsTimeout(rootEx) ? AnomalyKind.Timeout
                : AnomalyKind.Error;
            WriteApiAnomaly(method, kind, stopwatch.ElapsedMilliseconds,
                errorType: rootEx.GetType().Name, errorMessage: SanitizeMessage(rootEx.Message));
        }

        private void WriteApiAnomaly(string method, AnomalyKind kind, long elapsedMs,
            int? thresholdMs = null, string? errorType = null, string? errorMessage = null)
        {
            if (_anomalyWriter == null || _sessionService == null) { return; }
            var session = _sessionService.Get(AccessToken);
            _anomalyWriter.Write(new ApiAnomalyEntry
            {
                UserId = session?.UserId,
                UserName = session?.UserName,
                CompanyId = session?.CompanyId,
                AccessToken = AccessToken == Guid.Empty ? null : AccessToken,
                ApiKeyId = NullIfEmpty(ApiKeyValidation.SysId),
                ApiKeyName = NullIfEmpty(ApiKeyValidation.SysName),
                Method = method,
                Kind = kind,
                ElapsedMs = elapsedMs > int.MaxValue ? int.MaxValue : (int)elapsedMs,
                ThresholdMs = thresholdMs,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                Source = method,
            });
        }

        /// <summary>
        /// Normalises an empty string to <c>null</c> so an audit column reads as "not applicable"
        /// rather than blank.
        /// </summary>
        /// <param name="value">The value to normalise.</param>
        private static string? NullIfEmpty(string? value)
            => string.IsNullOrEmpty(value) ? null : value;

        private static bool IsTimeout(Exception ex)
            => ex is TimeoutException
               || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);

        private static string SanitizeMessage(string message)
        {
            // Message text only (no stack trace); flattened and capped.
            var oneLine = message.Replace('\r', ' ').Replace('\n', ' ');
            return oneLine.Length <= 1000 ? oneLine : oneLine[..1000];
        }

        #endregion

        /// <summary>
        /// Gets the API encryption key.
        /// </summary>
        /// <param name="format">The payload encoding format for transmission.</param>
        private byte[]? GetApiEncryptionKey(PayloadFormat format)
        {
            return format == PayloadFormat.Encrypted
                ? _keyProvider.GetKey(AccessToken)
                : null;
        }

        /// <summary>
        /// Parses the progId and action from the Method property.
        /// </summary>
        /// <returns>A tuple containing the progId and action. Throws if the format is invalid.</returns>
        private static (string progId, string action) ParseMethod(string method)
        {
            if (!string.IsNullOrEmpty(method))
            {
                var parts = method.Split(s_methodSeparators, 2);
                if (parts.Length == 2)
                {
                    return (parts[0], parts[1]);
                }
            }
            throw new FormatException($"Invalid method format: {method}");
        }

        /// <summary>
        /// Resolves the <see cref="MethodInfo"/> for the specified action on the given business object.
        /// </summary>
        /// <param name="businessObject">The business object instance.</param>
        /// <param name="action">The action name.</param>
        private static MethodInfo GetMethod(object businessObject, string action)
        {
            var method = businessObject.GetType().GetMethod(action);
            if (method == null)
                throw new MissingMethodException($"Method '{action}' not found in business object '{businessObject.GetType().Name}'.");
            return method;
        }

        /// <summary>
        /// Converts the input argument and asynchronously invokes the specified method on the business object.
        /// </summary>
        /// <param name="businessObject">The business object instance.</param>
        /// <param name="method">The resolved method to invoke.</param>
        /// <param name="value">The deserialized input argument.</param>
        private static async Task<object?> InvokeMethodAsync(object businessObject, MethodInfo method, object? value)
        {
            // Convert the input parameter to the expected BO type if needed
            var methodParams = method.GetParameters();
            if (methodParams.Length > 0 && value != null)
            {
                var paramType = methodParams[0].ParameterType;
                value = ApiInputConverter.Convert(value, paramType);
            }

            var result = method.Invoke(businessObject, new object?[] { value });

            // If the method is asynchronous (Task or Task<T>), await it
            if (result is Task task)
            {
                // Await the asynchronous task to completion (ConfigureAwait(false) recommended in server-side environments to avoid deadlocks)
                await task.ConfigureAwait(false);
                // If it is Task<T>, extract the Result; otherwise it is Task (void) and returns null
                var taskType = task.GetType();
                var isGeneric = taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>);
                return isGeneric
                    ? taskType.GetProperty("Result")?.GetValue(task)
                    : null;
            }

            return result;
        }

        /// <summary>
        /// Maps an exception to the corresponding JSON-RPC error code and message used in
        /// the response envelope. User-facing exceptions surface their original message;
        /// infrastructure exceptions return a generic message to avoid leaking internals.
        /// </summary>
        /// <param name="ex">The exception (already unwrapped) to map.</param>
        /// <returns>A tuple of the JSON-RPC error code and the message to expose.</returns>
        /// <remarks>
        /// <para>
        /// Which exception travels as which code is declared once, in
        /// <see cref="JsonRpcErrorContract"/>, and the client rebuilds from that same declaration.
        /// What stays here is only what the contract deliberately leaves out: the fallback for an
        /// exception it does not cover.
        /// </para>
        /// <para>
        /// Exposed as <c>internal</c> for direct unit testing through
        /// <c>InternalsVisibleTo</c>; the mapping is a protocol-level contract, not an
        /// implementation detail.
        /// </para>
        /// <para>
        /// In debug mode the infrastructure message is passed through instead of being replaced.
        /// The generic message is the right answer in production — an infrastructure failure
        /// should not describe the server's internals to a caller — but it leaves a developer
        /// with nothing to work from: the executor handles the exception here rather than letting
        /// it reach the transport, so nothing further up gets a chance to report it either. The
        /// same trade-off is already made at the transport layer, where
        /// <c>ApiServiceController</c> attaches the real message only when the host is running
        /// in development.
        /// </para>
        /// <para>
        /// WARNING: the debug branch must stay gated on <see cref="SysInfo.IsDebugMode"/>, and
        /// must pass the message alone. A stack trace or any wider dump would leak internal paths
        /// and system detail into an API response, which <c>rules/scanning.md</c> prohibits
        /// outright.
        /// </para>
        /// </remarks>
        internal static (JsonRpcErrorCode code, string message) MapException(Exception ex)
        {
            if (JsonRpcErrorContract.TryGetCode(ex, out var code))
                return (code, ex.Message);
            return (JsonRpcErrorCode.InternalError,
                SysInfo.IsDebugMode ? ex.Message : "Internal server error");
        }

        /// <summary>
        /// Creates an instance of the business object for the specified progId.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <returns>The business object instance.</returns>
        /// <remarks>
        /// No dispatch of its own: the reserved progIds resolve through the same registry as every
        /// other, so the transport layer has nothing left to decide. It used to branch on
        /// <c>System</c> and <c>AuditLog</c>, which meant two identifiers were progIds on the wire
        /// but not in the registry.
        /// </remarks>
        private object CreateBusinessObject(Guid accessToken, string progId)
        {
            if (string.IsNullOrWhiteSpace(progId))
                throw new ArgumentException("ProgId cannot be null or empty.", nameof(progId));

            return _boFactory.CreateBusinessObject(accessToken, progId, IsLocalCall);
        }
    }

}
