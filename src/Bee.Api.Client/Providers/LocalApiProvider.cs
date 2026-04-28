using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Client.Providers
{
    /// <summary>
    /// Local API service provider that accesses backend business logic directly within the same process.
    /// </summary>
    public class LocalApiProvider : IJsonRpcProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalApiProvider"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public LocalApiProvider(Guid accessToken)
        {
            AccessToken = accessToken;
        }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        public Guid AccessToken { get; }

        /// <summary>
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        public async Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
        {
            // Execute the API method
            var executor = new JsonRpcExecutor(AccessToken, true);
            var response = await executor.ExecuteAsync(request);
            return response;
        }
    }
}
