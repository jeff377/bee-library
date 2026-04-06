using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Output result for retrieving a single package (ZIP).
    /// </summary>
    [MessagePackObject]
    public sealed class GetPackageResult : BusinessResult
    {
        /// <summary>
        /// Gets or sets the file name (e.g., client-main-win-x64-1.2.4.zip).
        /// </summary>
        [Key(100)]
        public string FileName { get; set; } = "package.zip";

        /// <summary>
        /// Gets or sets the ZIP file byte content (used when Delivery = Api; large files should use URL download instead).
        /// </summary>
        [Key(101)]
        public byte[] Content { get; set; } = new byte[0];

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [Key(102)]
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the SHA256 hash of the file (hex string) for post-download integrity verification.
        /// </summary>
        [Key(103)]
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the short-lived download URL (used when Delivery = Url).
        /// </summary>
        [Key(104)]
        public string PackageUrl { get; set; } = string.Empty;
    }
}
