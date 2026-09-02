using Bee.Api.Core;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Tracing;
using Bee.Api.Client.Providers;
using Bee.Api.Core.Conversion;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Transformers;


namespace Bee.Api.Client.Connectors
{
    /// <summary>
    /// Base class for API service connectors.
    /// </summary>
    public abstract class ApiConnector
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConnector"/> class using a local connection.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        protected ApiConnector(Guid accessToken)
            : this(accessToken, ApiSessionContext.Ambient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConnector"/> class using a local connection
        /// and the given session state.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="session">
        /// The per-session state. A host serving several users from one process must give each session
        /// its own instance; sharing one makes the last login's transmission key overwrite the rest.
        /// </param>
        protected ApiConnector(Guid accessToken, ApiSessionContext session)
        {
            ArgumentNullException.ThrowIfNull(session);
            AccessToken = accessToken;
            Session = session;
            Provider = new LocalApiProvider(accessToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConnector"/> class using a remote connection.
        /// </summary>
        /// <param name="endpoint">The API service endpoint.</param>
        /// <param name="accessToken">The access token.</param>
        protected ApiConnector(string endpoint, Guid accessToken)
            : this(endpoint, accessToken, ApiSessionContext.Ambient)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConnector"/> class using a remote connection
        /// and the given session state.
        /// </summary>
        /// <param name="endpoint">The API service endpoint.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="session">
        /// The per-session state. This is the overload a multi-user host wants: the remote path is the
        /// one that encrypts payloads, so a shared context there is what locks users out of each other's
        /// sessions.
        /// </param>
        protected ApiConnector(string endpoint, Guid accessToken, ApiSessionContext session)
        {
            if (StringUtilities.IsEmpty(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));
            ArgumentNullException.ThrowIfNull(session);

            AccessToken = accessToken;
            Session = session;
            Provider = new RemoteApiProvider(endpoint, accessToken);
        }

        #endregion

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// Gets the per-session state this connector reads and writes.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="ApiSessionContext.Ambient"/> for connectors created through the
        /// constructors that do not take one, which is what keeps single-user hosts unchanged.
        /// </remarks>
        public ApiSessionContext Session { get; } = ApiSessionContext.Ambient;

        /// <summary>
        /// Gets or sets the API service provider.
        /// </summary>
        public IJsonRpcProvider Provider { get; private set; }

        /// <summary>
        /// Gets or sets the body codec this connector speaks, blank for the framework default
        /// (MessagePack).
        /// </summary>
        /// <remarks>
        /// Set it to <see cref="PayloadCodecNames.Json"/> to put the bodies of this connector's
        /// calls on the JSON codec. Only the serialization step changes: compression, encryption
        /// and the anti-replay frame are unaffected, and a Plain call carries no encoded body at
        /// all, so this has no effect on one.
        /// <para>
        /// The name is stamped on the request and the server answers in the same codec. A server
        /// too old to read the field falls back to MessagePack and fails to decode the body, which
        /// is the intended outcome — a mismatch is refused rather than silently misread.
        /// </para>
        /// </remarks>
        public string PayloadCodec { get; set; } = string.Empty;

        /// <summary>
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="action">The action name to execute.</param>
        /// <param name="value">The input parameter for the action.</param>
        /// <param name="format">The payload encoding format for transmission.</param>
        protected async Task<T> ExecuteAsync<T>(string progId, string action, object value, PayloadFormat format)
        {
            ValidateArgs(progId, action);
            var ctx = Tracer.Start(TraceLayers.ApiClient, string.Empty, name: $"ExecuteAsync.{progId}.{action}");
            try
            {
                // The Connector is the only place time zones are applied (ADR-032 D4): the wire is UTC
                // in both directions, so the request converts out of the user's zone here and the
                // response converts back into it. The swap is undone before returning so the caller's
                // own request object is left exactly as it was handed over.
                var timeZoneId = UserTimeZoneId;
                T result;
                using (PayloadZoneConverter.ToUtc(value, timeZoneId))
                {
                    var (request, actualFormat) = PrepareRequest(progId, action, value, format);

                    // Invoke the JSON-RPC method (remote or local)
                    var response = await this.Provider.ExecuteAsync(request).ConfigureAwait(false);

                    result = FinalizeResponse<T>(response, actualFormat);
                }
                PayloadZoneConverter.ToUserZone(result, timeZoneId);
                Tracer.End(ctx);
                return result;
            }
            catch (Exception ex)
            {
                // Boundary: record any failure of the remote/local call on the trace span, then
                // rethrow unchanged (preserving the stack). A catch-all is intentional here.
                Tracer.End(ctx, TraceStatus.Error, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// The signed-in user's IANA time zone id, or blank (meaning no conversion) before login.
        /// </summary>
        /// <remarks>
        /// Read per call rather than captured: a connector instance outlives a sign-in, and a stale
        /// zone would silently shift another user's data (ADR-032 D13).
        /// </remarks>
        private string UserTimeZoneId => Session.UserTimeZoneId;

        /// <summary>
        /// Validates the progId and action arguments.
        /// </summary>
        private static void ValidateArgs(string progId, string action)
        {
            if (StringUtilities.IsEmpty(progId))
                throw new ArgumentException("progId cannot be null or empty.", nameof(progId));
            if (StringUtilities.IsEmpty(action))
                throw new ArgumentException("action cannot be null or empty.", nameof(action));
        }

        /// <summary>
        /// Builds the JSON-RPC request, traces it, and transforms its payload to the target format.
        /// </summary>
        private (JsonRpcRequest request, PayloadFormat actualFormat) PrepareRequest(
            string progId, string action, object value, PayloadFormat format)
        {
            var request = CreateRequest(progId, action, value);
            // Guard before any transform: in-process calls skip serialization entirely, so this is
            // the only point both transports pass through (ADR-032 D6).
            DateTimeWireGuard.Validate(value);
            TraceRequest(request);
            var actualFormat = TransformRequestPayload(request, format);
            return (request, actualFormat);
        }

        /// <summary>
        /// Traces the response, checks for errors, restores the payload, and converts the result value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Error mapping reverses what the executor did on the way out, and both directions read
        /// the same declaration in <see cref="JsonRpcErrorContract"/> — a code that declares an
        /// exception type is rebuilt as that type carrying the original message with no prefix, so
        /// callers can <c>catch</c> the type instead of comparing integers. Everything else wraps
        /// into <see cref="InvalidOperationException"/> with the legacy
        /// <c>"API error: {code} - {message}"</c> format to preserve existing catch logic.
        /// </para>
        /// <para>
        /// Adding a new exception type to the wire is therefore one edit, in the contract. It used
        /// to be two, in two assemblies, with nothing tying them together — which is how
        /// <see cref="JsonRpcErrorCode.ReplayRejected"/> came to be produced by the server and
        /// silently dropped into the generic branch here, leaving the <c>catch</c> that type's own
        /// documentation promises unreachable.
        /// </para>
        /// </remarks>
        private T FinalizeResponse<T>(JsonRpcResponse response, PayloadFormat actualFormat)
        {
            TraceResponse(response);
            if (response.Error != null)
            {
                if (JsonRpcErrorContract.TryRebuild(response.Error.Code, response.Error.Message, out var rebuilt))
                    throw rebuilt;
                throw new InvalidOperationException($"API error: {response.Error.Code} - {response.Error.Message}");
            }
            RestoreResponsePayload(response, actualFormat);
            var result = ApiOutputConverter.ConvertResultValue<T>(response.Result!.Value!)!;
            DateTimeWireGuard.Validate(result);
            return result;
        }

        /// <summary>
        /// Creates a JSON-RPC request object.
        /// </summary>
        /// <param name="progId">The program identifier (e.g., Employee, Login).</param>
        /// <param name="action">The action name to invoke (e.g., Hello, GetList).</param>
        /// <param name="value">The parameter object to pass to the server.</param>
        /// <returns>The composed JSON-RPC request object.</returns>
        private static JsonRpcRequest CreateRequest(string progId, string action, object value)
        {
            return new JsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new JsonRpcParams
                {
                    Value = value
                },
                Id = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// Transforms the specified JSON-RPC request payload to the target transmission format (Plain, Encoded, or Encrypted).
        /// </summary>
        /// <param name="request">The JSON-RPC request object to process.</param>
        /// <param name="format">
        /// The desired payload format:
        /// <list type="bullet">
        /// <item><description><see cref="PayloadFormat.Plain"/>: No transformation.</description></item>
        /// <item><description><see cref="PayloadFormat.Encoded"/>: Serialize and compress.</description></item>
        /// <item><description><see cref="PayloadFormat.Encrypted"/>: Serialize, compress, and encrypt.</description></item>
        /// </list>
        /// </param>
        /// <returns>The actual format applied, which may be downgraded to Plain depending on the runtime environment.</returns>
        private PayloadFormat TransformRequestPayload(JsonRpcRequest request, PayloadFormat format)
        {
            // For local providers in non-debug mode, force Plain format to skip encoding/encryption and improve performance.
            if (this.Provider is LocalApiProvider && !SysInfo.IsDebugMode)
            {
                format = PayloadFormat.Plain; // No encoding in local non-debug mode
            }

            // If Encrypted is requested but no encryption key is set, downgrade to Encoded to prevent encryption failure.
            if (format == PayloadFormat.Encrypted && ValueUtilities.IsEmpty(Session.ApiEncryptionKey))
            {
                format = PayloadFormat.Encoded;
            }

            if (format != PayloadFormat.Plain)
            {
                // Stamped before the transform, which reads it off the payload the same way the
                // receiving end does.
                request.Params.Codec = PayloadCodec;

                if (ApiServiceOptions.RequireWireFrame)
                {
                    // Numbered here rather than inside the converter: only the connector knows
                    // which session the call belongs to, and the counter is per session.
                    request.Params.Frame = new ApiPayloadFrame(
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Session.NextSequence());
                }
                ApiPayloadConverter.TransformTo(request.Params, format, Session.ApiEncryptionKey);
            }

            return format;
        }

        /// <summary>
        /// Restores the JSON-RPC response payload by decoding or decrypting it back to the original object.
        /// </summary>
        /// <param name="response">The JSON-RPC response object to restore.</param>
        /// <param name="format">
        /// The response payload format:
        /// <list type="bullet">
        /// <item><description><see cref="PayloadFormat.Plain"/>: No processing; used as-is.</description></item>
        /// <item><description><see cref="PayloadFormat.Encoded"/> or <see cref="PayloadFormat.Encrypted"/>: Decode or decrypt the payload.</description></item>
        /// </list>
        /// </param>
        private void RestoreResponsePayload(JsonRpcResponse response, PayloadFormat format)
        {
            if (format == PayloadFormat.Plain)
                return;

            ApiPayloadConverter.RestoreFrom(response.Result!, format, Session.ApiEncryptionKey);
        }

        /// <summary>
        /// Traces the JSON-RPC request model.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        private static void TraceRequest(JsonRpcRequest request)
        {
            if (!Tracer.Enabled || request == null) return;
            Tracer.Write(TraceLayers.ApiClient, string.Empty, TraceStatus.Ok, TraceCategories.JsonRpc, request, name: $"Request  - {request.Method}");
        }

        /// <summary>
        /// Traces the JSON-RPC response model.
        /// </summary>
        /// <param name="response">The JSON-RPC response model.</param>
        private static void TraceResponse(JsonRpcResponse response)
        {
            if (!Tracer.Enabled || response == null) return;
            Tracer.Write(TraceLayers.ApiClient, string.Empty, TraceStatus.Ok, TraceCategories.JsonRpc, response, name: $"Response - {response.Method}");
        }

    }
}
