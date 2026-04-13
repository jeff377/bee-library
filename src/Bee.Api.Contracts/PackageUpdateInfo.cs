using MessagePack;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Update information for a single query item.
    /// </summary>
    [MessagePackObject]
    public class PackageUpdateInfo
    {
        /// <summary>
        /// Gets or sets the corresponding application or tool identifier.
        /// </summary>
        [Key(0)]
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the corresponding component identifier.
        /// </summary>
        [Key(1)]
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// Gets or sets a value indicating whether an update is available.
        /// </summary>
        [Key(2)]
        public bool UpdateAvailable { get; set; }

        /// <summary>
        /// Gets or sets the latest version string (e.g., 1.2.4).
        /// </summary>
        [Key(3)]
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the update is mandatory (true means the user must upgrade before continuing).
        /// </summary>
        [Key(4)]
        public bool Mandatory { get; set; }

        /// <summary>
        /// Gets or sets the package size in bytes.
        /// </summary>
        [Key(5)]
        public long PackageSize { get; set; }

        /// <summary>
        /// Gets or sets the SHA256 hash of the package file (hex string) for post-download integrity verification.
        /// </summary>
        [Key(6)]
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the delivery mode (Url or Api).
        /// </summary>
        [Key(7)]
        public PackageDelivery Delivery { get; set; } = PackageDelivery.Url;

        /// <summary>
        /// Gets or sets the short-lived download URL when Delivery is Url.
        /// </summary>
        [Key(8)]
        public string PackageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the release summary or release notes (plain text or Markdown).
        /// </summary>
        [Key(9)]
        public string ReleaseNotes { get; set; } = string.Empty;

        // Add new fields starting from Key(10)
    }
}
