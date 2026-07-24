using System.Collections.Concurrent;
using Bee.Definition;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Default <see cref="ICacheContainerProvider"/>: builds one override
    /// <see cref="CacheContainerService"/> per customization code on demand and caches it in a
    /// thread-safe <see cref="ConcurrentDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// The override containers reuse the existing cache classes unchanged; only their backing
    /// storage (<see cref="CustomizeOnlyStorage"/>) and cache prefix differ from the base layer.
    /// The base container is never created or touched here.
    /// </remarks>
    public sealed class CacheContainerProvider : ICacheContainerProvider
    {
        private readonly PathOptions _paths;
        private readonly ConcurrentDictionary<string, ICacheContainer> _containers = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new <see cref="CacheContainerProvider"/>.
        /// </summary>
        /// <param name="paths">The host path options; <see cref="PathOptions.CustomizePath"/> roots every override container.</param>
        public CacheContainerProvider(PathOptions paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <inheritdoc/>
        public ICacheContainer For(string customizeId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(customizeId);
            return _containers.GetOrAdd(customizeId, CreateContainer);
        }

        private ICacheContainer CreateContainer(string customizeId)
        {
            var custPaths = new CustomizeOnlyPathOptions(_paths.CustomizePath, customizeId);
            var storage = new CustomizeOnlyStorage(custPaths);
            return new CacheContainerService(storage, custPaths, customizeId);
        }
    }
}
