using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A grid layout for tabular data.
    /// </summary>
    [Serializable]
    [XmlType("LayoutGrid")]
    [Description("Grid layout for tabular data.")]
    [TreeNode]
    public class LayoutGrid : LayoutItemBase
    {
        private LayoutColumnCollection _columns = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="LayoutGrid"/>.
        /// </summary>
        public LayoutGrid()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="LayoutGrid"/>.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="displayName">The display name.</param>
        public LayoutGrid(string tableName, string displayName)
        {
            TableName = tableName;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Table name.")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the actions allowed on the grid control.
        /// </summary>
        [XmlAttribute]
        [Description("Actions allowed on the grid control.")]
        [DefaultValue(GridControlAllowActions.All)]
        public GridControlAllowActions AllowActions { get; set; } = GridControlAllowActions.All;

        /// <summary>
        /// Gets the column collection.
        /// </summary>
        [Description("Column collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        public LayoutColumnCollection Columns
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _columns)) { return null; }
                if (_columns == null) { _columns = new LayoutColumnCollection(); }
                return _columns;
            }
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_columns, serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.TableName} - {this.DisplayName}";
        }
    }
}
