using Bee.Definition.Collections;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A grid layout column.
    /// </summary>
    [Serializable]
    [XmlType("LayoutColumn")]
    [Description("Grid layout column.")]
    [TreeNode]
    public class LayoutColumn : CollectionItem
    {
        private ListItemCollection? _listItems = null;
        private PropertyCollection? _extendedProperties = null;

        /// <summary>
        /// Initializes a new instance of <see cref="LayoutColumn"/>.
        /// </summary>
        public LayoutColumn()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="LayoutColumn"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="controlType">The control type.</param>
        public LayoutColumn(string fieldName, string caption, ColumnControlType controlType)
        {
            FieldName = fieldName;
            Caption = caption;
            ControlType = controlType;
        }

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
        public ColumnControlType ControlType { get; set; } = ColumnControlType.TextEdit;

        /// <summary>
        /// Gets or sets the related program ID.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("Related program ID.")]
        [DefaultValue("")]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this column is visible.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Indicates whether this column is visible.")]
        [DefaultValue(true)]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this column is read-only.
        /// </summary>
        [Category(PropertyCategories.Appearance)]
        [XmlAttribute]
        [Description("Indicates whether this column is read-only.")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the column width. A value greater than 0 is required to take effect.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Column width. A value greater than 0 is required to take effect.")]
        [DefaultValue(0)]
        public int Width { get; set; } = 0;

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
        /// Gets the list item collection.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [Description("List item collection.")]
        [DefaultValue(null)]
        public ListItemCollection? ListItems
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _listItems!)) { return null; }
                if (_listItems == null) { _listItems = []; }
                return _listItems;
            }
        }

        /// <summary>
        /// Gets the extended property collection.
        /// </summary>
        [Description("Extended property collection.")]
        [DefaultValue(null)]
        public PropertyCollection? ExtendedProperties
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _extendedProperties!)) { return null; }
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
            _listItems?.SetSerializeState(serializeState);
            _extendedProperties?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.FieldName} - {this.Caption}";
        }
    }
}
