namespace Bee.Definition.Storage
{
    /// <summary>
    /// Describes how a consumer can tell that a stored define has changed. Each
    /// <see cref="IDefineStorage"/> implementation reports the signal its own backing provides.
    /// </summary>
    /// <remarks>
    /// This is deliberately a description of the storage's change signal, not a cache policy: the
    /// caching layer translates it into its own policy type. Keeping the translation on the caching
    /// side is what allows <c>Bee.Definition</c> to stay free of any dependency on the cache layer.
    /// </remarks>
    public readonly record struct DefineChangeSource
    {
        /// <summary>
        /// File-backed signal: modification of any of these files means the define has changed.
        /// <c>null</c> when the storage has no watchable file backing.
        /// </summary>
        public string[]? FilePaths { get; init; }

        /// <summary>
        /// Database-backed signal: the cache-notify key whose version bump means the define has
        /// changed. <c>null</c> when the storage does not publish change notifications.
        /// </summary>
        public string? NotifyKey { get; init; }

        /// <summary>
        /// No detectable change signal — the consumer can only fall back on time-based expiry.
        /// </summary>
        public static DefineChangeSource None => default;
    }
}
