using Bee.Definition.Collections;
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A layout item.
    /// </summary>
    [Serializable]
    [XmlType("LayoutItem")]
    [Description("Layout item.")]
    [TreeNode]
    public class LayoutItem : LayoutItemBase
    {
        private int _rowSpan = 1;
        private int _columnSpan = 1;
        private ListItemCollection _listItems = null;
        private PropertyCollection _extendedProperties = null;

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
        public ControlType ControlType { get; set; } = ControlType.TextEdit;

        /// <summary>
        /// Gets or sets the number of rows to span.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Number of rows to span.")]
        [DefaultValue(1)]
        public int RowSpan
        {
            get { return _rowSpan; }
            set
            {
                if (value < 1) { value = 1; }
                _rowSpan = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of columns to span.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Number of columns to span.")]
        [DefaultValue(1)]
        public int ColumnSpan
        {
            get { return _columnSpan; }
            set
            {
                if (value < 1) { value = 1; }
                _columnSpan = value;
            }
        }

        /// <summary>
        /// Gets or sets the related program ID.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("Related program ID.")]
        [DefaultValue("")]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this item is read-only.
        /// </summary>
        [Category(PropertyCategories.Appearance)]
        [XmlAttribute]
        [Description("Indicates whether this item is read-only.")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; } = false;

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
        public ListItemCollection ListItems
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _listItems)) { return null; }
                if (_listItems == null) { _listItems = new ListItemCollection(); }
                return _listItems;
            }
        }

        /// <summary>
        /// Gets the extended property collection.
        /// </summary>
        [Description("Extended property collection.")]
        [DefaultValue(null)]
        public PropertyCollection ExtendedProperties
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _extendedProperties)) { return null; }
                if (_extendedProperties == null) { _extendedProperties = new PropertyCollection(); }
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
            BaseFunc.SetSerializeState(_listItems, serializeState);
            BaseFunc.SetSerializeState(_extendedProperties, serializeState);
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
