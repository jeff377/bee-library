using MessagePack;

namespace Bee.Definition.Collections
{
    /// <summary>
    /// A parameter item collection with serialization support.
    /// </summary>
    [MessagePackObject]
    public class ParameterCollection : MessagePackKeyCollectionBase<Parameter>
    {
        /// <summary>
        /// Gets the value of a parameter.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <param name="name">The parameter name.</param>
        public T GetValue<T>(string name)
        {
            if (this.Contains(name))
                return (T)this[name].Value!;
            else
                throw new KeyNotFoundException($"Parameter '{name}' does not exist.");
        }

        /// <summary>
        /// Gets the value of a parameter, returning a default value if the parameter does not exist.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <param name="name">The parameter name.</param>
        /// <param name="defaultValue">The default value to return if the parameter does not exist.</param>
        public T GetValue<T>(string name, T defaultValue)
        {
            if (this.Contains(name))
                return (T)this[name].Value!;
            else
                return defaultValue;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="ParameterCollection"/>.
    /// </summary>
    public static class ParameterCollectionExtensions
    {
        /// <summary>
        /// Adds a parameter. If a parameter with the same name already exists, its value is updated.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        public static void Add(this ParameterCollection? collection, string name, object value)
        {
            ArgumentNullException.ThrowIfNull(collection);
            if (collection.Contains(name))
            {
                collection[name].Value = value;
            }
            else
            {
                collection.Add(new Parameter(name, value));
            }
        }
    }
}
