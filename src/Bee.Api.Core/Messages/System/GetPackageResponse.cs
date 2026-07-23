using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get package operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public sealed class GetPackageResponse : ApiResponse, IGetPackageResponse
    {
        /// <summary>
        /// Gets or sets the file name of the package.
        /// </summary>
        public string FileName { get; set; } = "package.zip";

        /// <summary>
        /// Gets or sets the package content bytes.
        /// </summary>
        public byte[] Content { get; set; } = global::System.Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the SHA-256 hash of the package.
        /// </summary>
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL for downloading the package.
        /// </summary>
        public string PackageUrl { get; set; } = string.Empty;
    }
}
