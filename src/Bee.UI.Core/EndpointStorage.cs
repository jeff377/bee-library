using Bee.Base.Serialization;

namespace Bee.UI.Core
{
    /// <summary>
    /// Default <see cref="IEndpointStorage"/> implementation backed by <see cref="ClientInfo.ClientSettings"/>.
    /// </summary>
    public class EndpointStorage : IEndpointStorage
    {
        /// <summary>
        /// Returns the persisted service endpoint.
        /// </summary>
        public string LoadEndpoint()
        {
            return ClientInfo.ClientSettings.Endpoint;
        }

        /// <summary>
        /// Updates the in-memory service endpoint without persisting it.
        /// </summary>
        /// <param name="endpoint">Service endpoint.</param>
        public void SetEndpoint(string endpoint)
        {
            ClientInfo.ClientSettings.Endpoint = endpoint;
        }

        /// <summary>
        /// Updates and persists the service endpoint.
        /// </summary>
        /// <param name="endpoint">Service endpoint.</param>
        public void SaveEndpoint(string endpoint)
        {
            ClientInfo.ClientSettings.Endpoint = endpoint;
            ClientInfo.ClientSettings.Save();
        }
    }
}
