using Bee.Definition.Api;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for retrieving a single package (ZIP).
    /// </summary>
    public sealed class GetPackageResult : BusinessResult, IGetPackageResponse
    {
        /// <summary>
        /// Gets or sets the file name (e.g., client-main-win-x64-1.2.4.zip).
        /// </summary>
        public string FileName { get; set; } = "package.zip";

        /// <summary>
        /// Gets or sets the ZIP file byte content (used when Delivery = Api; large files should use URL download instead).
        /// </summary>
        public byte[] Content { get; set; } = new byte[0];

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the SHA256 hash of the file (hex string) for post-download integrity verification.
        /// </summary>
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the short-lived download URL (used when Delivery = Url).
        /// </summary>
        public string PackageUrl { get; set; } = string.Empty;
    }
}
