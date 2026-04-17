using System;
using System.Threading.Tasks;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Client.ApiServiceProvider
{
    /// <summary>
    /// Local API service provider that accesses backend business logic directly within the same process.
    /// </summary>
    public class LocalApiServiceProvider : IJsonRpcProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalApiServiceProvider"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public LocalApiServiceProvider(Guid accessToken)
        {
            AccessToken = accessToken;
        }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        public Guid AccessToken { get; }

        /// <summary>
        /// Executes an API method synchronously.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        /// <remarks>
        /// Prefer <see cref="ExecuteAsync"/> for better performance and resource utilization.
        /// </remarks>
#pragma warning disable CS0618 // Implementing obsolete interface member
        public JsonRpcResponse Execute(JsonRpcRequest request)
#pragma warning restore CS0618
        {
            // Execute the API method
            var executor = new JsonRpcExecutor(AccessToken, true);
            var response = executor.Execute(request);
            return response;
        }

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
