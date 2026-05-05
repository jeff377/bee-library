using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Collections;
using Bee.Base.Serialization;
using Bee.Definition.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Abstract base class for layout fields.
    /// Holds the rendering attributes shared by <see cref="LayoutField"/> (master section field)
    /// and <see cref="LayoutColumn"/> (grid column).
    /// </summary>
    [Description("Layout field base class.")]
    public abstract class LayoutFieldBase : CollectionItem
    {
        private PropertyCollection? _extendedProperties = null;

        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Field name.")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the caption text.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Caption text.")]
        [DefaultValue("")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the control type.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Control type.")]
        [DefaultValue(ControlType.TextEdit)]
        public ControlType ControlType { get; set; } = ControlType.TextEdit;

        /// <summary>
        /// Gets or sets the display format string.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("Display format string.")]
        [DefaultValue("")]
        public string DisplayFormat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number format string.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("Number format string.")]
        [DefaultValue("")]
        public string NumberFormat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this field is visible.
        /// Layout-level visibility: false means the field exists in the layout
        /// (e.g. for grid row binding) but is not rendered.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Indicates whether this field is visible.")]
        [DefaultValue(true)]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this field is read-only.
        /// </summary>
        [Category(PropertyCategories.Appearance)]
        [XmlAttribute]
        [Description("Indicates whether this field is read-only.")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// Gets the extended property collection.
        /// </summary>
        [Description("Extended property collection.")]
        [DefaultValue(null)]
        public PropertyCollection? ExtendedProperties
        {
            get
            {
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _extendedProperties!)) { return null; }
                if (_extendedProperties == null) { _extendedProperties = []; }
                return _extendedProperties;
            }
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            _extendedProperties?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{FieldName} - {Caption}";
        }
    }
}
