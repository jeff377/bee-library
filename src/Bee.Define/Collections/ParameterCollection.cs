using System;
using System.Collections.Generic;
using MessagePack;

namespace Bee.Define.Collections
{
    /// <summary>
    /// A parameter item collection with serialization support.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class ParameterCollection : MessagePackKeyCollectionBase<Parameter>
    {
        /// <summary>
        /// Adds a parameter. If a parameter with the same name already exists, its value is updated.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        public void Add(string name, object value)
        {
            if (this.Contains(name))
            {
                this[name].Value = value;
            }
            else
            {
                this.Add(new Parameter(name, value));
            }
        }

        /// <summary>
        /// Gets the value of a parameter.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <param name="name">The parameter name.</param>
        public T GetValue<T>(string name)
        {
            if (this.Contains(name))
                return (T)this[name].Value;
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
                return (T)this[name].Value;
            else
                return defaultValue;
        }
    }
}
