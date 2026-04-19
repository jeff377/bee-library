using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Collections
{
    /// <summary>
    /// A custom property.
    /// </summary>
    [Serializable]
    [XmlType("Property")]
    [Description("Custom property.")]
    public class Property : KeyCollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="Property"/>.
        /// </summary>
        public Property()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="Property"/>.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        public Property(string name, string value)
        {
            Name = name;
            Value = value;
        }

        #endregion

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        [XmlAttribute]
        [Description("Property name.")]
        public string Name
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        [XmlAttribute]
        [Description("Property value.")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}
