using Bee.Base;
using Bee.Definition;
using Bee.Definition.Language;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.Define
{
    /// <summary>
    /// Language resource cache, keyed by <c>"{lang}.{namespace}"</c> composite.
    /// One cache entry per (language, namespace) pair — invalidating a single namespace
    /// in one language does not affect the others.
    /// </summary>
    public class LanguageResourceCache : KeyObjectCache<LanguageResource>
    {
        private readonly IDefineStorage _storage;
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new instance of <see cref="LanguageResourceCache"/>.
        /// </summary>
        /// <param name="storage">The define storage backing this cache.</param>
        /// <param name="paths">Path options used for file-change monitoring when <paramref name="storage"/> is a <see cref="FileDefineStorage"/>.</param>
        /// <param name="cachePrefix">Per-owner cache namespace (see <see cref="KeyObjectCache{T}"/>).</param>
        public LanguageResourceCache(IDefineStorage storage, PathOptions paths, string cachePrefix = "") : base(cachePrefix)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the cache item expiration policy.
        /// </summary>
        /// <param name="key">The member key in the format <c>"{lang}.{namespace}"</c>.</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            // Parse the member key to extract the language code and namespace.
            key.SplitLeft(".", out string lang, out string ns);

            // Default: sliding expiration of 20 minutes (matches other Define caches).
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (_storage is FileDefineStorage)
                policy.ChangeMonitorFilePaths = new string[] { _paths.GetLanguageFilePath(lang, ns) };
            return policy;
        }

        /// <summary>
        /// Creates an instance of the language resource.
        /// </summary>
        /// <param name="key">The member key in the format <c>"{lang}.{namespace}"</c>.</param>
        protected override LanguageResource? CreateInstance(string key)
        {
            key.SplitLeft(".", out string lang, out string ns);
            return _storage.GetLanguage(lang, ns);
        }

        /// <summary>
        /// Gets the language resource for the specified language and namespace.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        public LanguageResource? Get(string lang, string ns)
        {
            string key = $"{lang}.{ns}";
            return base.Get(key);
        }

        /// <summary>
        /// Removes the language resource entry from the cache.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        public void Remove(string lang, string ns)
        {
            string key = $"{lang}.{ns}";
            base.Remove(key);
        }
    }
}
