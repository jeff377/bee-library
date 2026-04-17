using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Bee.Api.Core;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Api.Client;

namespace Bee.Api.Client.ApiServiceProvider
{
    /// <summary>
    /// Remote API service provider that accesses backend business logic over the network.
    /// </summary>
    public class RemoteApiServiceProvider : IJsonRpcProvider
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteApiServiceProvider"/> class.
        /// </summary>
        /// <param name="endpoint">The API service endpoint.</param>
        /// <param name="accessToken">The access token.</param>
        public RemoteApiServiceProvider(string endpoint, Guid accessToken)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));

            Endpoint = endpoint;
            AccessToken = accessToken;  // Note: AccessToken may be Guid.Empty for unauthenticated calls (e.g., Login, Ping)
        }

        #endregion

        /// <summary>
        /// Gets or sets the service endpoint.
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        public Guid AccessToken { get; } = Guid.Empty;

        /// <summary>
        /// Executes an API method synchronously.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        /// <remarks>
        /// Prefer <see cref="ExecuteAsync"/> for better performance and resource utilization.
        /// This synchronous wrapper uses <c>Task.Run</c> to avoid UI thread deadlocks,
        /// but may cause ThreadPool pressure under high concurrency on server-side scenarios.
        /// </remarks>
#pragma warning disable CS0618 // Implementing obsolete interface member
        public JsonRpcResponse Execute(JsonRpcRequest request)
#pragma warning restore CS0618
        {
            // Running async on the current thread risks deadlock (especially on UI threads); no scheduling overhead; suitable only when the caller is guaranteed not to be a UI thread.
            // return ExecuteAsync(request).GetAwaiter().GetResult();

            // Running async on a new thread lowers deadlock risk (UI thread is not blocked); incurs scheduling overhead; suitable when the caller may be a UI thread.
            return Task.Run(() => ExecuteAsync(request)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        public async Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
        {
            var headers = CreateHeaders();
            string body = request.ToJson();  // Serialize input parameters to JSON
            string json = await HttpFunc.PostAsync(Endpoint, body, headers).ConfigureAwait(false); // Call the Web API
            var response = SerializeFunc.JsonToObject<JsonRpcResponse>(json);  // Deserialize JSON response
            return response!;
        }

        /// <summary>
        /// Creates the HTTP header collection for the request.
        /// </summary>
        private NameValueCollection CreateHeaders()
        {
            return new NameValueCollection
            {
                { ApiHeaders.ApiKey, ApiClientContext.ApiKey },
                { ApiHeaders.Authorization, $"Bearer {AccessToken}" }
            };
        }

    }
}
