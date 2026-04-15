using System;
using System.Threading.Tasks;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Client.ApiServiceProvider
{
    /// <summary>
    /// Interface for a JSON-RPC service provider.
    /// </summary>
    public interface IJsonRpcProvider
    {
        /// <summary>
        /// Executes an API method synchronously.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        /// <remarks>
        /// Prefer <see cref="ExecuteAsync"/> for better performance and resource utilization.
        /// This synchronous method is retained only for UI thread compatibility.
        /// </remarks>
        [Obsolete("Use ExecuteAsync for better performance. This synchronous method is retained only for UI thread compatibility.")]
        JsonRpcResponse Execute(JsonRpcRequest request);

        /// <summary>
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request);
    }
}
