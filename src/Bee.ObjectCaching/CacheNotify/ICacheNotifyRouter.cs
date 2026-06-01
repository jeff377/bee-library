namespace Bee.ObjectCaching.CacheNotify
{
    /// <summary>
    /// Static routing table mapping a cache group (the prefix of a <c>"group:entity"</c>
    /// cache key, e.g. <c>"OrgInfo"</c> in <c>"OrgInfo:0001"</c>) to the cache-eviction action
    /// performed when the cache-notify poller observes a version bump for that key.
    /// </summary>
    /// <remarks>
    /// Routes are declared in code (compile-time type checking, refactor-friendly) alongside the
    /// cache registrations. A group with no registered route is a no-op, and evicting a key that
    /// is not currently cached is itself a no-op, so the router needs no de-registration state.
    /// </remarks>
    public interface ICacheNotifyRouter
    {
        /// <summary>
        /// Registers the eviction action for a cache group. Registering the same group again
        /// replaces the previous action.
        /// </summary>
        /// <param name="cacheGroup">
        /// The cache group, i.e. the part of a cache key before the first <c>:</c>
        /// (e.g. <c>"OrgInfo"</c>, <c>"FormSchema"</c>).
        /// </param>
        /// <param name="evict">
        /// The eviction action invoked with the resolved <see cref="ICacheContainer"/> and the
        /// entity portion of the cache key (the part after the first <c>:</c>).
        /// </param>
        void Register(string cacheGroup, Action<ICacheContainer, string> evict);

        /// <summary>
        /// Resolves the route for <paramref name="cacheKey"/> and invokes its eviction action.
        /// </summary>
        /// <param name="container">The cache container the eviction action operates on.</param>
        /// <param name="cacheKey">The full <c>"group:entity"</c> cache key from the notification table.</param>
        /// <returns>
        /// <c>true</c> when a route matched the key's group and its action ran; <c>false</c> when
        /// no route is registered for the group (the bump is ignored).
        /// </returns>
        bool TryInvoke(ICacheContainer container, string cacheKey);
    }
}
