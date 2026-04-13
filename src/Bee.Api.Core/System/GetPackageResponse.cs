using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API response for the get package operation.
    /// </summary>
    [MessagePackObject]
    public sealed class GetPackageResponse : ApiResponse, IGetPackageResponse
    {
        /// <summary>
        /// Gets or sets the file name of the package.
        /// </summary>
        [Key(100)]
        public string FileName { get; set; } = "package.zip";

        /// <summary>
        /// Gets or sets the package content bytes.
        /// </summary>
        [Key(101)]
        public byte[] Content { get; set; } = new byte[0];

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [Key(102)]
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the SHA-256 hash of the package.
        /// </summary>
        [Key(103)]
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL for downloading the package.
        /// </summary>
        [Key(104)]
        public string PackageUrl { get; set; } = string.Empty;
    }
}
