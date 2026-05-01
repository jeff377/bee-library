using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A field mapping that maps a source field to a destination field.
    /// </summary>
    [XmlType("FieldMapping")]
    [Description("Field mapping.")]
    public class FieldMapping : CollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="FieldMapping"/>.
        /// </summary>
        public FieldMapping()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="FieldMapping"/>.
        /// </summary>
        /// <param name="sourceField">The source field.</param>
        /// <param name="destinationField">The destination field.</param>
        public FieldMapping(string sourceField, string destinationField)
        {
            SourceField = sourceField;
            DestinationField = destinationField;
        }

        #endregion

        /// <summary>
        /// Gets or sets the source field.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Source field.")]
        public string SourceField { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the destination field.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Destination field.")]
        public string DestinationField { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{SourceField} -> {DestinationField}";
        }
    }
}
