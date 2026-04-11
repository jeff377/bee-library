using MessagePack;

namespace Bee.Definition.Api
{
    /// <summary>
    /// Package query item specifying which update to check (App/Component + current version + platform/channel).
    /// </summary>
    [MessagePackObject]
    public class PackageUpdateQuery
    {
        /// <summary>
        /// Gets or sets the application or tool identifier (e.g., Client, SettingsEditor, DefinitionTool, DbUpgrade, FlowService).
        /// </summary>
        [Key(0)]
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component identifier (e.g., Main, Reference, Plugin-XYZ). Defaults to Main when not specified.
        /// </summary>
        [Key(1)]
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// Gets or sets the currently installed version string (e.g., 1.2.3). Pass an empty string for a first-time installation.
        /// </summary>
        [Key(2)]
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target platform (e.g., Win-x64, Win-arm64, macOS).
        /// </summary>
        [Key(3)]
        public string Platform { get; set; } = "Win-x64";

        /// <summary>
        /// Gets or sets the release channel (e.g., Stable, Beta).
        /// </summary>
        [Key(4)]
        public string Channel { get; set; } = "Stable";
    }
}
