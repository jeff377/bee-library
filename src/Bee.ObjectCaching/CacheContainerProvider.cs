using System.Collections.Concurrent;
using Bee.Definition;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Supplies per-customization-code override cache containers. Each container is backed by a
    /// <see cref="CustomizeOnlyStorage"/> rooted at <c>{CustomizePath}/{customizeId}/</c> and uses
    /// <c>CachePrefix = customizeId</c> so tenant data is physically isolated over the shared
    /// process-wide cache provider.
    /// </summary>
    public interface ICacheContainerProvider
    {
        /// <summary>
        /// Gets (lazily creating on first use) the override cache container for the given
        /// customization code. Repeated calls with the same code return the same container.
        /// </summary>
        /// <param name="customizeId">The non-empty tenant customization code.</param>
        ICacheContainer For(string customizeId);
    }

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
