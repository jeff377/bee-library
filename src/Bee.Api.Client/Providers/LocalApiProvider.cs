using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Client.Providers
{
    /// <summary>
    /// Local API service provider that accesses backend business logic directly within the same process.
    /// </summary>
    /// <remarks>
    /// Near-end mode requires a fully built backend service provider — set via
    /// <see cref="ApiClientInfo.LocalServiceProvider"/> once at startup. The provider resolves a
    /// <see cref="JsonRpcExecutor"/> per request to honour JsonRpcExecutor's transient lifetime.
    /// Phase 4 transitional: <c>Bee.Api.Client</c> near-end mode keeps a static service-provider
    /// holder until a follow-up phase migrates it to constructor injection (out-of-scope per main plan).
    /// </remarks>
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
            var services = ApiClientInfo.LocalServiceProvider
                ?? throw new InvalidOperationException(
                    "ApiClientInfo.LocalServiceProvider is not configured. " +
                    "Local API calls require an in-process backend; assign a service provider " +
                    "built via services.AddBeeFramework(configuration).BuildServiceProvider() before use.");
            var executor = services.GetService(typeof(JsonRpcExecutor)) as JsonRpcExecutor
                ?? throw new InvalidOperationException(
                    "JsonRpcExecutor is not registered in ApiClientInfo.LocalServiceProvider.");
            executor.AccessToken = AccessToken;
            executor.IsLocalCall = true;
            return await executor.ExecuteAsync(request);
        }
    }
}
