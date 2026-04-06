using Bee.Define.Settings;
using System;
using System.IO;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Connect.Connectors;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// Validator for API service connection settings.
    /// </summary>
    public class ApiConnectValidator
    {
        /// <summary>
        /// Validates the input service endpoint and returns the corresponding connection type.
        /// </summary>
        /// <param name="endpoint">The endpoint to validate: a URL for remote connections or a local path for local connections.</param>
        /// <param name="allowGenerateSettings">Whether to auto-generate missing settings files (SystemSettings.xml and DatabaseSettings.xml) for local connections.</param>
        public ConnectType Validate(string endpoint, bool allowGenerateSettings = false)
        {
            if (StrFunc.IsEmpty(endpoint))
                throw new ArgumentException("Input cannot be null or empty.", nameof(endpoint));

            if (FileFunc.IsLocalPath(endpoint))  // Local path: validate local connection settings
            {
                // Validate local connection settings
                ValidateLocal(endpoint, allowGenerateSettings);
                // Return local connection type
                return ConnectType.Local;
            }
            else if (HttpFunc.IsUrl(endpoint))    // URL: validate remote connection settings
            {
                // Validate remote connection settings
                ValidateRemote(endpoint);
                // Return remote connection type
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
        private void ValidateLocal(string definePath, bool allowGenerateSettings)
        {
            // Verify the application supports local connections
            if (!ApiClientContext.SupportedConnectTypes.HasFlag(SupportedConnectTypes.Local))
                throw new InvalidOperationException("Local connections are not supported.");
            if (StrFunc.IsEmpty(definePath))
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
                if (!FileFunc.DirectoryExists(definePath))
                    throw new ArgumentException("Definition path does not exist.", nameof(definePath));
                // Verify that SystemSettings.xml exists in the specified path
                string filePath = FileFunc.PathCombine(definePath, "SystemSettings.xml");
                if (!FileFunc.FileExists(filePath))
                    throw new FileNotFoundException("SystemSettings.xml file not found in the definition path.", filePath);
            }
        }

        /// <summary>
        /// Verifies that SystemSettings.xml exists in the definition path, creating it if missing.
        /// </summary>
        /// <param name="definePath">The definition path.</param>
        private void ValidateSystemSettings(string definePath)
        {
            // Check for SystemSettings.xml; create it if not found
            string filePath = FileFunc.PathCombine(definePath, "SystemSettings.xml");
            if (!FileFunc.FileExists(filePath))
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
        private void ValidateDatabaseSettings(string definePath)
        {
            // Check for DatabaseSettings.xml; create it if not found
            string filePath = FileFunc.PathCombine(definePath, "DatabaseSettings.xml");
            if (!FileFunc.FileExists(filePath))
            {
                var settings = new DatabaseSettings();
                var item = new DatabaseItem()
                {
                    Id = "default",
                    DisplayName = "Default Database"
                };
                settings.Items.Add(item);
                settings.SetObjectFilePath(filePath);
                settings.Save();
            }
        }

        /// <summary>
        /// Validates the remote connection settings.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        private void ValidateRemote(string endpoint)
        {
            // Verify the application supports remote connections
            if (!ApiClientContext.SupportedConnectTypes.HasFlag(SupportedConnectTypes.Remote))
                throw new InvalidOperationException("Remote connections are not supported.");
            if (StrFunc.IsEmpty(endpoint))
                throw new ArgumentException("The endpoint must be specified.", nameof(endpoint));
            // Use remote connection to execute the Ping method
            var connector = new SystemApiConnector(endpoint, Guid.Empty);
            connector.Ping();
        }
    }
}
