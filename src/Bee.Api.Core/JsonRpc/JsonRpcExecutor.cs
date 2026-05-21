using System.Reflection;
using Bee.Base.Exceptions;
using Bee.Base.Tracing;
using Bee.Definition;
using Bee.Definition.Security;
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
        private static readonly char[] MethodSeparators = new[] { '.' };

        private readonly IBusinessObjectFactory _boFactory;
        private readonly IAccessTokenValidator _tokenValidator;
        private readonly IApiEncryptionKeyProvider _keyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcExecutor"/> class.
        /// </summary>
        /// <param name="boFactory">The business-object factory.</param>
        /// <param name="tokenValidator">The access-token validator.</param>
        /// <param name="keyProvider">The API encryption key provider.</param>
        public JsonRpcExecutor(
            IBusinessObjectFactory boFactory,
            IAccessTokenValidator tokenValidator,
            IApiEncryptionKeyProvider keyProvider)
        {
            _boFactory = boFactory ?? throw new ArgumentNullException(nameof(boFactory));
            _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
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
        /// Executes an API method.
        /// </summary>
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
            try
            {
                var format = request.Params.Format;

                // Parse method, create BO, and validate access BEFORE decryption.
                // This ensures unauthenticated or unauthorized requests are rejected without
                // performing any decryption work.
                var (progId, action) = ParseMethod(request.Method);
                var businessObject = CreateBusinessObject(AccessToken, progId);
                var method = GetMethod(businessObject, action);
                ApiAccessValidator.ValidateAccess(method, new ApiCallContext(AccessToken, IsLocalCall, format), _tokenValidator);

                // Access confirmed: retrieve the encryption key and decrypt the payload.
                byte[]? apiEncryptionKey = GetApiEncryptionKey(format);
                ApiPayloadConverter.RestoreFrom(request.Params, format, apiEncryptionKey);

                // Invoke the method and convert the result.
                var value = await InvokeMethodAsync(businessObject, method, request.Params.Value);
                value = ApiOutputConverter.Convert(value!);

                response.Result = new JsonRpcResult { Value = value };
                ApiPayloadConverter.TransformTo(response.Result, format, apiEncryptionKey);
                Tracer.End(ctx);
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
            }
            return response;
        }

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
                var parts = method.Split(MethodSeparators, 2);
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
        /// Returns true for exception types whose message is safe to surface to API clients.
        /// Business-layer and validation exceptions are user-facing; infrastructure exceptions are not.
        /// </summary>
        /// <remarks>
        /// <c>UserMessageException</c> is the preferred type for new code. BCL exceptions
        /// remain on the whitelist as a transition path; they are scheduled for gradual
        /// removal once business code has migrated.
        /// </remarks>
        private static bool IsUserFacingException(Exception ex)
        {
            return ex is UserMessageException
                || ex is UnauthorizedAccessException
                || ex is ArgumentException          // includes ArgumentNullException, ArgumentOutOfRangeException
                || ex is InvalidOperationException
                || ex is NotSupportedException
                || ex is FormatException
                || ex is JsonRpcException;
        }

        /// <summary>
        /// Maps an exception to the corresponding JSON-RPC error code and message used in
        /// the response envelope. User-facing exceptions surface their original message;
        /// infrastructure exceptions return a generic message to avoid leaking internals.
        /// </summary>
        /// <param name="ex">The exception (already unwrapped) to map.</param>
        /// <returns>A tuple of the JSON-RPC error code and the message to expose.</returns>
        /// <remarks>
        /// Exposed as <c>internal</c> for direct unit testing through
        /// <c>InternalsVisibleTo</c>; the mapping is a protocol-level contract, not an
        /// implementation detail.
        /// </remarks>
        internal static (JsonRpcErrorCode code, string message) MapException(Exception ex)
        {
            if (IsUserFacingException(ex))
                return (JsonRpcErrorCode.UserMessage, ex.Message);
            return (JsonRpcErrorCode.InternalError, "Internal server error");
        }

        /// <summary>
        /// Creates an instance of the business object for the specified progId.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <returns>The business object instance.</returns>
        private object CreateBusinessObject(Guid accessToken, string progId)
        {
            if (string.IsNullOrWhiteSpace(progId))
                throw new ArgumentException("ProgId cannot be null or empty.", nameof(progId));

            if (progId == SysProgIds.System)
                return _boFactory.CreateSystemBusinessObject(accessToken, IsLocalCall);
            else
                return _boFactory.CreateFormBusinessObject(accessToken, progId, IsLocalCall);
        }
    }

}
