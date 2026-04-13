using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API request for the get package operation.
    /// </summary>
    [MessagePackObject]
    public sealed class GetPackageRequest : ApiRequest, IGetPackageRequest
    {
        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        [Key(100)]
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the component identifier.
        /// </summary>
        [Key(101)]
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// Gets or sets the requested version.
        /// </summary>
        [Key(102)]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target platform.
        /// </summary>
        [Key(103)]
        public string Platform { get; set; } = "Win-x64";

        /// <summary>
        /// Gets or sets the update channel.
        /// </summary>
        [Key(104)]
        public string Channel { get; set; } = "Stable";

        /// <summary>
        /// Gets or sets the specific file identifier.
        /// </summary>
        [Key(105)]
        public string FileId { get; set; } = string.Empty;
    }
}
