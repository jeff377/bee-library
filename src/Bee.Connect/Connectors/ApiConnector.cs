using Bee.Api.Core;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Tracing;
using Bee.Connect.ApiServiceProvider;
using System;
using System.Threading.Tasks;

namespace Bee.Connect.Connectors
{
    /// <summary>
    /// Base class for API service connectors.
    /// </summary>
    public abstract class ApiConnector
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConnector"/> class using a local connection.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public ApiConnector(Guid accessToken)
        {
            AccessToken = accessToken;
            Provider = new LocalApiServiceProvider(accessToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConnector"/> class using a remote connection.
        /// </summary>
        /// <param name="endpoint">The API service endpoint.</param>
        /// <param name="accessToken">The access token.</param>
        public ApiConnector(string endpoint, Guid accessToken)
        {
            if (StrFunc.IsEmpty(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));

            AccessToken = accessToken;
            Provider = new RemoteApiServiceProvider(endpoint, accessToken);
        }

        #endregion

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// Gets or sets the API service provider.
        /// </summary>
        public IJsonRpcProvider Provider { get; private set; }

        /// <summary>
        /// Executes an API method.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="action">The action name to execute.</param>
        /// <param name="value">The input parameter for the action.</param>
        /// <param name="format">The payload encoding format for transmission.</param>
        protected T Execute<T>(string progId, string action, object value, PayloadFormat format)
        {
            if (StrFunc.IsEmpty(progId))
                throw new ArgumentException("progId cannot be null or empty.", nameof(progId));
            if (StrFunc.IsEmpty(action))
                throw new ArgumentException("action cannot be null or empty.", nameof(action));

            var ctx = Tracer.Start(TraceLayer.ApiClient, string.Empty, $"Execute.{progId}.{action}");
            try
            {
                // Build the JSON-RPC request model
                var request = CreateRequest(progId, action, value);
                TraceRequest(request);

                // Transform the payload to the specified format
                var actualFormat = TransformRequestPayload(request, format);

                // Invoke the JSON-RPC method (remote or local)
                var response = this.Provider.Execute(request);
                TraceResponse(response);

                if (response.Error != null)
                    throw new InvalidOperationException($"API error: {response.Error.Code} - {response.Error.Message}");

                // Restore the response payload (if Encoded or Encrypted)
                RestoreResponsePayload(response, actualFormat);

                Tracer.End(ctx);
                return (T)response.Result.Value;
            }
            catch (Exception ex)
            {
                Tracer.End(ctx, TraceStatus.Error, ex.Message);
                throw;
            }


        }

        /// <summary>
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="action">The action name to execute.</param>
        /// <param name="value">The input parameter for the action.</param>
        /// <param name="format">The payload encoding format for transmission.</param>
        protected async Task<T> ExecuteAsync<T>(string progId, string action, object value, PayloadFormat format)
        {
            if (StrFunc.IsEmpty(progId))
                throw new ArgumentException("progId cannot be null or empty.", nameof(progId));
            if (StrFunc.IsEmpty(action))
                throw new ArgumentException("action cannot be null or empty.", nameof(action));

            var ctx = Tracer.Start(TraceLayer.ApiClient, string.Empty, $"ExecuteAsync.{progId}.{action}");
            try
            {
                // Build the JSON-RPC request model
                var request = CreateRequest(progId, action, value);
                TraceRequest(request);

                // Transform the payload to the specified format
                var actualFormat = TransformRequestPayload(request, format);

                // Invoke the JSON-RPC method (remote or local)
                var response = await this.Provider.ExecuteAsync(request).ConfigureAwait(false);
                TraceResponse(response);

                if (response.Error != null)
                    throw new InvalidOperationException($"API error: {response.Error.Code} - {response.Error.Message}");

                // Restore the response payload (if Encoded or Encrypted)
                RestoreResponsePayload(response, actualFormat);

                Tracer.End(ctx);
                return (T)response.Result.Value;
            }
            catch (Exception ex)
            {
                Tracer.End(ctx, TraceStatus.Error, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates a JSON-RPC request object.
        /// </summary>
        /// <param name="progId">The program identifier (e.g., Employee, Login).</param>
        /// <param name="action">The action name to invoke (e.g., Hello, GetList).</param>
        /// <param name="value">The parameter object to pass to the server.</param>
        /// <returns>The composed JSON-RPC request object.</returns>
        private JsonRpcRequest CreateRequest(string progId, string action, object value)
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
            if (this.Provider is LocalApiServiceProvider && !SysInfo.IsDebugMode)
            {
                format = PayloadFormat.Plain; // No encoding in local non-debug mode
            }

            // If Encrypted is requested but no encryption key is set, downgrade to Encoded to prevent encryption failure.
            if (format == PayloadFormat.Encrypted && BaseFunc.IsEmpty(ApiClientContext.ApiEncryptionKey))
            {
                format = PayloadFormat.Encoded;
            }

            if (format != PayloadFormat.Plain)
            {
                ApiPayloadConverter.TransformTo(request.Params, format, ApiClientContext.ApiEncryptionKey);
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

            ApiPayloadConverter.RestoreFrom(response.Result, format, ApiClientContext.ApiEncryptionKey);
        }

        /// <summary>
        /// Traces the JSON-RPC request model.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        private void TraceRequest(JsonRpcRequest request)
        {
            if (!Tracer.Enabled || request == null) return;
            Tracer.Write(TraceLayer.ApiClient, string.Empty, $"Request  - {request.Method}", TraceStatus.Ok, TraceCategories.JsonRpc, request);
        }

        /// <summary>
        /// Traces the JSON-RPC response model.
        /// </summary>
        /// <param name="response">The JSON-RPC response model.</param>
        private void TraceResponse(JsonRpcResponse response)
        {
            if (!Tracer.Enabled || response == null) return;
            Tracer.Write(TraceLayer.ApiClient, string.Empty, $"Response - {response.Method}", TraceStatus.Ok, TraceCategories.JsonRpc, response);
        }

    }
}
