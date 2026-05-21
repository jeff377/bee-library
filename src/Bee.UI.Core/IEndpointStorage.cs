namespace Bee.UI.Core
{
    /// <summary>
    /// Persistence contract for the service endpoint configured by the client.
    /// </summary>
    public interface IEndpointStorage
    {
        /// <summary>
        /// Returns the persisted service endpoint.
        /// </summary>
        string LoadEndpoint();

        /// <summary>
        /// Updates the in-memory service endpoint without persisting it.
        /// </summary>
        /// <param name="endpoint">Service endpoint.</param>
        void SetEndpoint(string endpoint);

        /// <summary>
        /// Updates and persists the service endpoint.
        /// </summary>
        /// <param name="endpoint">Service endpoint.</param>
        void SaveEndpoint(string endpoint);
    }
}
