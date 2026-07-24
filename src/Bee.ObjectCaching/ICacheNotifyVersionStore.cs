namespace Bee.ObjectCaching
{
    /// <summary>
    /// Holds the latest observed cache-notify version per notify key.
    /// </summary>
    /// <remarks>
    /// The cache-notify poller writes the versions it reads from the notify table; cache entries
    /// carrying a <see cref="CacheItemPolicy.ChangeNotifyKey"/> compare against them to detect that
    /// another process changed the underlying data. This mirrors how a file-backed entry compares
    /// the file's last-write time, so both invalidation kinds behave the same way.
    /// </remarks>
    public interface ICacheNotifyVersionStore
    {
        /// <summary>
        /// Returns the latest observed version for the key, or <c>0</c> when nothing has been observed.
        /// </summary>
        /// <param name="notifyKey">The cache-notify key (<c>"{group}:{entity}"</c>).</param>
        long GetVersion(string notifyKey);

        /// <summary>
        /// Records the latest observed version for the key.
        /// </summary>
        /// <param name="notifyKey">The cache-notify key (<c>"{group}:{entity}"</c>).</param>
        /// <param name="version">The version read from the notify table.</param>
        void SetVersion(string notifyKey, long version);
    }
}
