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
}
