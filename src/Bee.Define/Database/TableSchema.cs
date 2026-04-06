using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define.Database
{
    /// <summary>
    /// Table schema.
    /// </summary>
    [Serializable]
    [XmlType("TableSchema")]
    [Description("Table schema.")]
    [TreeNode]
    public class TableSchema : IObjectSerializeFile
    {
        private DbFieldCollection _fields = null;
        private TableSchemaIndexCollection _indexes = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="TableSchema"/>.
        /// </summary>
        public TableSchema()
        {
        }

        #endregion

        #region IObjectSerializeFile Interface

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_fields, serializeState);
            BaseFunc.SetSerializeState(_indexes, serializeState);
        }

        /// <summary>
        /// Gets the serialization-bound file path.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Sets the serialization-bound file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// Gets the object creation time.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("Table name.")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the field collection.
        /// </summary>
        [Description("Field collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        public DbFieldCollection Fields
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _fields)) { return null; }
                if (_fields == null) { _fields = new DbFieldCollection(this); }
                return _fields;
            }
        }

        /// <summary>
        /// Gets the index collection.
        /// </summary>
        [Description("Index collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        public TableSchemaIndexCollection Indexes
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _indexes)) { return null; }
                if (_indexes == null) { _indexes = new TableSchemaIndexCollection(this); }
                return _indexes;
            }
        }

        /// <summary>
        /// Gets the primary key index.
        /// </summary>
        public TableSchemaIndex GetPrimaryKey()
        {
            foreach (TableSchemaIndex index in Indexes)
            {
                if (index.PrimaryKey)
                    return index;
            }
            return null;
        }

        /// <summary>
        /// Gets or sets the table schema upgrade action.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [DefaultValue(DbUpgradeAction.None)]
        public DbUpgradeAction UpgradeAction { get; set; } = DbUpgradeAction.None;

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        public TableSchema Clone()
        {
            var table = new TableSchema();
            table.TableName = TableName;
            table.DisplayName = DisplayName;
            foreach (TableSchemaIndex index in Indexes)
                table.Indexes.Add(index.Clone());
            foreach (DbField field in Fields)
                table.Fields.Add(field.Clone());
            return table;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{TableName} - {DisplayName}";
        }
    }
}
