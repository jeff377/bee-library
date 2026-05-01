using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A layout group.
    /// </summary>
    [Serializable]
    [XmlType("LayoutGroup")]
    [Description("Layout group.")]
    [TreeNode]
    public class LayoutGroup : CollectionItem
    {
        private int _columnCount = 1;
        private LayoutItemCollection? _items = null;

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Group name.")]
        [DefaultValue("")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the caption text.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Caption text.")]
        [DefaultValue("")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the caption is shown.
        /// </summary>
        [XmlAttribute]
        [Description("Indicates whether the caption is shown.")]
        [DefaultValue(true)]
        public bool ShowCaption { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of columns.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Number of columns.")]
        public int ColumnCount
        {
            get { return _columnCount; }
            set
            {
                if (value < 1) { value = 1; }
                _columnCount = value;
            }
        }

        /// <summary>
        /// Gets the layout item collection.
        /// </summary>
        [Description("Layout item collection.")]
        [Browsable(false)]
        [XmlArrayItem(typeof(LayoutItem))]
        [XmlArrayItem(typeof(LayoutGrid))]
        [DefaultValue(null)]
        public LayoutItemCollection? Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _items!)) { return null; }
                if (_items == null) { _items = []; }
                return _items;
            }
        }

        /// <summary>
        /// Finds the grid layout for the specified table name.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        public LayoutGrid? FindGrid(string tableName)
        {
            foreach (LayoutItemBase item in this.Items!)
            {
                if (item is LayoutGrid grid && StringUtilities.IsEquals(grid.TableName, tableName))
                    return grid;
            }
            return null;
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            _items?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.Name} - {this.Caption}";
        }
    }
}
