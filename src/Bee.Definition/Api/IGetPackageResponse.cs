namespace Bee.Definition.Api
{
    /// <summary>
    /// Contract interface for get package response data.
    /// </summary>
    public interface IGetPackageResponse
    {
        /// <summary>
        /// Gets the file name of the package.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Gets the package content bytes.
        /// </summary>
        byte[] Content { get; }

        /// <summary>
        /// Gets the file size in bytes.
        /// </summary>
        long FileSize { get; }

        /// <summary>
        /// Gets the SHA-256 hash of the package.
        /// </summary>
        string Sha256 { get; }

        /// <summary>
        /// Gets the URL for downloading the package.
        /// </summary>
        string PackageUrl { get; }
    }
}
