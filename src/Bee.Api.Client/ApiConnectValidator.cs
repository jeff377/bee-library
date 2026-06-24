using Bee.Definition.Settings;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Api.Client.Connectors;

namespace Bee.Api.Client
{
    /// <summary>
    /// Validator for API service connection settings.
    /// </summary>
    public static class ApiConnectValidator
    {
        /// <summary>
        /// Validates the input service endpoint and returns the corresponding connection type.
        /// </summary>
        /// <param name="endpoint">The endpoint to validate: a URL for remote connections or a local path for local connections.</param>
        /// <param name="allowGenerateSettings">Whether to auto-generate missing settings files (SystemSettings.xml and DatabaseSettings.xml) for local connections.</param>
        /// <remarks>
        /// Remote validation awaits the ping and reachability probes instead of blocking on them,
        /// so it is safe on single-threaded runtimes (browser WASM) where blocking would throw
        /// "Cannot wait on monitors".
        /// </remarks>
        public static async Task<ConnectType> ValidateAsync(string endpoint, bool allowGenerateSettings = false)
        {
            if (StringUtilities.IsEmpty(endpoint))
                throw new ArgumentException("Input cannot be null or empty.", nameof(endpoint));

            if (FileUtilities.IsLocalPath(endpoint))
            {
                // Local validation is pure file-system I/O with no async work to await.
                ValidateLocal(endpoint, allowGenerateSettings);
                return ConnectType.Local;
            }
            else if (HttpUtilities.IsUrl(endpoint))
            {
                await ValidateRemoteAsync(endpoint).ConfigureAwait(false);
                return ConnectType.Remote;
            }
            else
            {
                throw new InvalidOperationException("Unrecognized connection type. Please enter a valid service endpoint or local path.");
            }
        }

        /// <summary>
        /// Validates the local connection settings.
        /// </summary>
        /// <param name="definePath">The definition path.</param>
        /// <param name="allowGenerateSettings">Whether to auto-generate missing settings files for local connections.</param>
        private static void ValidateLocal(string definePath, bool allowGenerateSettings)
        {
            // Verify the application supports local connections
            if (!ApiClientInfo.SupportedConnectTypes.HasFlag(SupportedConnectTypes.Local))
                throw new InvalidOperationException("Local connections are not supported.");
            if (StringUtilities.IsEmpty(definePath))
                throw new ArgumentException("Definition path must be specified.", nameof(definePath));

            if (allowGenerateSettings) // Auto-generate missing settings files (used by tool applications)
            {
                // Verify SystemSettings.xml exists; create it if missing
                ValidateSystemSettings(definePath);
                // Verify DatabaseSettings.xml exists; create it if missing
                ValidateDatabaseSettings(definePath);
            }
            else // Settings files must already exist (used by regular applications)
            {
                if (!Directory.Exists(definePath))
                    throw new ArgumentException("Definition path does not exist.", nameof(definePath));
                // Verify that SystemSettings.xml exists in the specified path
                string filePath = Path.Combine(definePath, "SystemSettings.xml");
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("SystemSettings.xml file not found in the definition path.", filePath);
            }
        }

        /// <summary>
        /// Verifies that SystemSettings.xml exists in the definition path, creating it if missing.
        /// </summary>
        /// <param name="definePath">The definition path.</param>
        private static void ValidateSystemSettings(string definePath)
        {
            // Check for SystemSettings.xml; create it if not found
            string filePath = Path.Combine(definePath, "SystemSettings.xml");
            if (!File.Exists(filePath))
            {
                var settings = new SystemSettings();
                settings.SetObjectFilePath(filePath);
                settings.Save();
            }
        }

        /// <summary>
        /// Verifies that DatabaseSettings.xml exists in the definition path, creating it if missing.
        /// </summary>
        /// <param name="definePath">The definition path.</param>
        private static void ValidateDatabaseSettings(string definePath)
        {
            // Check for DatabaseSettings.xml; create it if not found
            string filePath = Path.Combine(definePath, "DatabaseSettings.xml");
            if (!File.Exists(filePath))
            {
                var settings = new DatabaseSettings();
                var item = new DatabaseItem()
                {
                    Id = "default",
                    DisplayName = "Default Database"
                };
                settings.Items!.Add(item);
                settings.SetObjectFilePath(filePath);
                settings.Save();
            }
        }

        /// <summary>
        /// Validates the remote connection settings, awaiting the reachability and ping probes.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        private static async Task ValidateRemoteAsync(string endpoint)
        {
            // Verify the application supports remote connections
            if (!ApiClientInfo.SupportedConnectTypes.HasFlag(SupportedConnectTypes.Remote))
                throw new InvalidOperationException("Remote connections are not supported.");
            if (StringUtilities.IsEmpty(endpoint))
                throw new ArgumentException("The endpoint must be specified.", nameof(endpoint));
            // Pre-check transport-level reachability before establishing the connector
            if (!await HttpUtilities.IsEndpointReachableAsync(endpoint).ConfigureAwait(false))
                throw new InvalidOperationException($"Endpoint not reachable: {endpoint}");
            // Use remote connection to execute the Ping method
            var connector = new SystemApiConnector(endpoint, Guid.Empty);
            await connector.PingAsync().ConfigureAwait(false);
        }
    }
}
