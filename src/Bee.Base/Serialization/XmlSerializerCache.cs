using System.Collections.Concurrent;
using System.Xml.Serialization;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Provides caching for <see cref="XmlSerializer"/> instances to improve serialization performance.
    /// </summary>
    public static class XmlSerializerCache
    {
        /// <summary>
        /// Cache of previously created <see cref="XmlSerializer"/> instances.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, XmlSerializer> _cache = new ConcurrentDictionary<Type, XmlSerializer>();

        /// <summary>
        /// Gets the <see cref="XmlSerializer"/> instance for the specified type, creating and caching it if not already present.
        /// </summary>
        /// <param name="type">The target serialization type.</param>
        /// <returns>The <see cref="XmlSerializer"/> instance for the specified type.</returns>
        public static XmlSerializer Get(Type type)
        {
            return _cache.GetOrAdd(type, t => new XmlSerializer(t));
        }
    }

}
