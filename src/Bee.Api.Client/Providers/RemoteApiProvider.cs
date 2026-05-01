using System.Collections.Specialized;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Api.Core.Messages;

namespace Bee.Api.Client.Providers
{
    /// <summary>
    /// Remote API service provider that accesses backend business logic over the network.
    /// </summary>
    public class RemoteApiProvider : IJsonRpcProvider
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteApiProvider"/> class.
        /// </summary>
        /// <param name="endpoint">The API service endpoint.</param>
        /// <param name="accessToken">The access token.</param>
        public RemoteApiProvider(string endpoint, Guid accessToken)
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
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="request">The JSON-RPC request model.</param>
        public async Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
        {
            var headers = CreateHeaders();
            string body = request.ToJson();  // Serialize input parameters to JSON
            string json = await HttpUtilities.PostAsync(Endpoint, body, headers).ConfigureAwait(false); // Call the Web API
            var response = JsonCodec.Deserialize<JsonRpcResponse>(json);  // Deserialize JSON response
            return response!;
        }

        /// <summary>
        /// Creates the HTTP header collection for the request.
        /// </summary>
        private NameValueCollection CreateHeaders()
        {
            return new NameValueCollection
            {
                { ApiHeaders.ApiKey, ApiClientInfo.ApiKey },
                { ApiHeaders.Authorization, $"Bearer {AccessToken}" }
            };
        }

    }
}
