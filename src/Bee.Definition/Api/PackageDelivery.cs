namespace Bee.Definition.Api
{
    /// <summary>
    /// Package delivery mode. Serialized as integer values (0/1); do not change the numeric values of existing members.
    /// </summary>
    public enum PackageDelivery : int
    {
        /// <summary>
        /// Returns a short-lived URL for direct download (recommended for large files).
        /// </summary>
        Url = 0,
        /// <summary>
        /// Returns the file content as bytes directly via the API (suitable for small files or internal environments).
        /// </summary>
        Api = 1
    }
}
