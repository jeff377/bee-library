using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the get package operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class GetPackageRequest : ApiRequest, IGetPackageRequest
    {
        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component identifier.
        /// </summary>
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// Gets or sets the requested version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target platform.
        /// </summary>
        public string Platform { get; set; } = "Win-x64";

        /// <summary>
        /// Gets or sets the update channel.
        /// </summary>
        public string Channel { get; set; } = "Stable";

        /// <summary>
        /// Gets or sets the specific file identifier.
        /// </summary>
        public string FileId { get; set; } = string.Empty;
    }
}
