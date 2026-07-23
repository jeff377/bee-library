using MessagePack;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Package query item specifying which update to check (App/Component + current version + platform/channel).
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class PackageUpdateQuery
    {
        /// <summary>
        /// Gets or sets the application or tool identifier (e.g., Client, SettingsEditor, DefinitionTool, DbUpgrade, FlowService).
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component identifier (e.g., Main, Reference, Plugin-XYZ). Defaults to Main when not specified.
        /// </summary>
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// Gets or sets the currently installed version string (e.g., 1.2.3). Pass an empty string for a first-time installation.
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target platform (e.g., Win-x64, Win-arm64, macOS).
        /// </summary>
        public string Platform { get; set; } = "Win-x64";

        /// <summary>
        /// Gets or sets the release channel (e.g., Stable, Beta).
        /// </summary>
        public string Channel { get; set; } = "Stable";
    }
}
