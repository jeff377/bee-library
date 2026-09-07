using System.Collections.Concurrent;
using Bee.ObjectCaching;
using Bee.ObjectCaching.Providers;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// A working in-memory <see cref="ICacheProvider"/> for testing the decorator without any
    /// framework bootstrap.
    /// </summary>
    /// <remarks>
    /// NOTE: Bee.ObjectCaching.UnitTests has a private nested provider of the same shape, but that
    /// one is an empty stub whose `Get` always returns null. Counting hits against it would be
    /// meaningless, so this stores values for real. The names deliberately differ to keep the two
    /// from being mistaken for each other.
    /// </remarks>
    internal sealed class InMemoryCacheProvider : ICacheProvider
    {
        private readonly ConcurrentDictionary<string, object> _items = new(StringComparer.Ordinal);

        public bool Contains(string key) => _items.ContainsKey(key);

        public object? Get(string key) => _items.TryGetValue(key, out var value) ? value : null;

        public void Set(string key, object value, CacheItemPolicy policy) => _items[key] = value;

        public void Remove(string key) => _items.TryRemove(key, out _);

        public long GetCount() => _items.Count;
    }
}
