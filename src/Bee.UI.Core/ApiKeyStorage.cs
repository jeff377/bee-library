using Bee.Base.Serialization;

namespace Bee.UI.Core
{
    /// <summary>
    /// Default <see cref="IApiKeyStorage"/> implementation backed by
    /// <see cref="ClientInfo.ClientSettings"/>, mirroring <see cref="EndpointStorage"/>.
    /// </summary>
    /// <remarks>
    /// The settings file lives beside the assembly, which is writable for console and unpackaged
    /// desktop hosts. Packaged and sandboxed hosts (iOS, Android, browser WASM) cannot write there
    /// and already replace <see cref="ClientInfo.EndpointStorage"/> for the same reason — they
    /// should replace <see cref="ClientInfo.ApiKeyStorage"/> too.
    /// </remarks>
    public class ApiKeyStorage : IApiKeyStorage
    {
        /// <summary>
        /// Returns the persisted API key.
        /// </summary>
        public string LoadApiKey()
        {
            return ClientInfo.ClientSettings.ApiKey;
        }

        /// <summary>
        /// Updates the in-memory API key without persisting it.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        public void SetApiKey(string apiKey)
        {
            ClientInfo.ClientSettings.ApiKey = apiKey;
        }

        /// <summary>
        /// Updates and persists the API key.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        public void SaveApiKey(string apiKey)
        {
            ClientInfo.ClientSettings.ApiKey = apiKey;
            ClientInfo.ClientSettings.Save();
        }
    }
}
