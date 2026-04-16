using System.Data;

namespace Bee.Base.Collections
{
    /// <summary>
    /// Extension methods for collections.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Gets the value associated with the specified key, or a default value if the key is not found.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="collection">The property collection.</param>
        /// <param name="key">The property key.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        public static T? GetValue<T>(this PropertyCollection collection, string key, object? defaultValue)
        {
            if (collection.Contains(key))
                return (T?)collection[key];
            else
                return (T?)defaultValue;
        }
    }
}
