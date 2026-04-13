using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Arguments for retrieving a single package (ZIP), identified precisely by AppId + ComponentId + Version + Platform + Channel.
    /// </summary>
    public sealed class GetPackageArgs : BusinessArgs, IGetPackageRequest
    {
        /// <summary>
        /// Gets or sets the application or tool identifier.
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component identifier (e.g., Main, Reference, Plugin-XYZ).
        /// </summary>
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// Gets or sets the version string to download (e.g., 1.2.4).
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target platform (e.g., Win-x64, Win-arm64, macOS).
        /// </summary>
        public string Platform { get; set; } = "Win-x64";

        /// <summary>
        /// Gets or sets the release channel (e.g., Stable, Beta).
        /// </summary>
        public string Channel { get; set; } = "Stable";

        /// <summary>
        /// Gets or sets the optional file identifier for distinguishing multiple variants of the same version (filename, storage key, or variant code).
        /// </summary>
        public string FileId { get; set; } = string.Empty;
    }
}
