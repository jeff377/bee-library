using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Collections;

namespace Bee.Definition.Collections
{
    /// <summary>
    /// A custom property collection.
    /// </summary>
    [Serializable]
    [Description("Custom property collection.")]
    public class PropertyCollection : KeyCollectionBase<Property>
    {
        /// <summary>
        /// Adds a new property to the collection.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        public void Add(string name, string value)
        {
            base.Add(new Property(name, value));
        }

        /// <summary>
        /// Gets the string value of a property.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        public string GetValue(string name, string defaultValue)
        {
            if (this.Contains(name))
                return this[name].Value;
            else
                return defaultValue;
        }

        /// <summary>
        /// Gets the boolean value of a property.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        public bool GetValue(string name, bool defaultValue)
        {
            if (this.Contains(name))
                return BaseFunc.CBool(this[name].Value);
            else
                return defaultValue;
        }

        /// <summary>
        /// Gets the integer value of a property.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        public int GetValue(string name, int defaultValue)
        {
            if (this.Contains(name))
                return BaseFunc.CInt(this[name].Value);
            else
                return defaultValue;
        }
    }
}
