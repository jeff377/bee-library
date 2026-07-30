namespace Bee.UI.Core
{
    /// <summary>
    /// Persistence contract for the API key the client presents as <c>X-Api-Key</c>.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="IEndpointStorage"/> rather than added to it: that
    /// interface's members are all named for the endpoint, and widening it would leave the name
    /// describing only half of what it carries. Hosts assign both properties on
    /// <see cref="ClientInfo"/>, and a platform storage class is free to implement both and be
    /// assigned to each — the two values share a medium on every platform in practice.
    /// <para>
    /// NOTE: an API key held by a client is not a secret in the cryptographic sense — it can be
    /// recovered from the shipped application. The goal here is that changing it does not require
    /// recompiling anything, not that it is hidden. User authentication remains the access token's
    /// job.
    /// </para>
    /// </remarks>
    public interface IApiKeyStorage
    {
        /// <summary>
        /// Returns the persisted API key, or an empty string when none has been stored.
        /// </summary>
        string LoadApiKey();

        /// <summary>
        /// Updates the in-memory API key without persisting it.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        void SetApiKey(string apiKey);

        /// <summary>
        /// Updates and persists the API key.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        void SaveApiKey(string apiKey);
    }
}
