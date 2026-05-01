using Bee.Base;
using Bee.Base.Tracing;
using Bee.Definition;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcExecutor"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        public JsonRpcExecutor(Guid accessToken, bool isLocalCall = false)
        {
            AccessToken = accessToken;
            IsLocalCall = isLocalCall;
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
                // Payload transmission format
                var format = request.Params.Format;
                // Get the API encryption key
                byte[]? apiEncryptionKey = GetApiEncryptionKey(format);
                // Restore the request payload content
                ApiPayloadConverter.RestoreFrom(request.Params, format, apiEncryptionKey);

                // Parse the ProgId and Action from the Method property
                var (progId, action) = ParseMethod(request.Method);
                // Create the business object and invoke the specified method
                var value = await ExecuteMethodAsync(progId, action, request.Params.Value, format);

                // Convert BO result to API response type by naming convention
                value = ApiOutputConverter.Convert(value!);

                // Return the result
                response.Result = new JsonRpcResult { Value = value };
                // Set the response payload format
                ApiPayloadConverter.TransformTo(response.Result, format, apiEncryptionKey);
                Tracer.End(ctx);
            }
            catch (Exception ex)
            {
                var rootEx = ex.Unwrap();
                // Only expose the exception message for known user-facing exception types.
                // System/infrastructure exceptions return a generic message to avoid leaking internals.
                string message = IsUserFacingException(rootEx) ? rootEx.Message : "Internal server error";
                response.Error = new JsonRpcError(-1, message);
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
                ? BackendInfo.ApiEncryptionKeyProvider.GetKey(AccessToken)
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
        /// Creates the business object and asynchronously executes the specified method.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="value">The input argument for the action.</param>
        /// <param name="format">The payload encoding format for transmission.</param>
        private async Task<object?> ExecuteMethodAsync(string progId, string action, object? value, PayloadFormat format)
        {
            // Create an instance of the business object for the specified progId
            var businessObject = CreateBusinessObject(AccessToken, progId);
            var method = businessObject.GetType().GetMethod(action);
            if (method == null)
                throw new MissingMethodException($"Method '{action}' not found in business object '{progId}'.");

            // Access validation
            ApiAccessValidator.ValidateAccess(method, new ApiCallContext(AccessToken, IsLocalCall, format));

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
        private static bool IsUserFacingException(Exception ex)
        {
            return ex is UnauthorizedAccessException
                || ex is ArgumentException          // includes ArgumentNullException, ArgumentOutOfRangeException
                || ex is InvalidOperationException
                || ex is NotSupportedException
                || ex is FormatException
                || ex is JsonRpcException;
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
                return BackendInfo.BusinessObjectFactory.CreateSystemBusinessObject(accessToken, IsLocalCall);
            else
                return BackendInfo.BusinessObjectFactory.CreateFormBusinessObject(accessToken, progId, IsLocalCall);
        }
    }

}
