using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Bee.Base.Collections;

namespace Bee.Define.Database
{
    /// <summary>
    /// Table index schema.
    /// </summary>
    [Serializable]
    [XmlType("TableSchemaIndex")]
    [Description("Table index schema.")]
    [TreeNode]
    public class TableSchemaIndex : KeyCollectionItem
    {
        private IndexFieldCollection _indexFields = null;

        /// <summary>
        /// Gets or sets the index name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("Index name.")]
        public string Name
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the index is unique.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Indicates whether the index is unique.")]
        [DefaultValue(false)]
        public bool Unique { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this is the primary key.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Indicates whether this is the primary key.")]
        [DefaultValue(false)]
        public bool PrimaryKey { get; set; } = false;

        /// <summary>
        /// Gets the index field collection.
        /// </summary>
        [Description("Index field collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        public IndexFieldCollection IndexFields
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _indexFields)) { return null; }
                if (_indexFields == null) { _indexFields = new IndexFieldCollection(); }
                return _indexFields;
            }
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_indexFields, serializeState);
        }

        /// <summary>
        /// Gets or sets the index schema upgrade action.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [DefaultValue(DbUpgradeAction.None)]
        public DbUpgradeAction UpgradeAction { get; set; } = DbUpgradeAction.None;

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        public TableSchemaIndex Clone()
        {
            var index = new TableSchemaIndex();
            index.Name = Name;
            index.PrimaryKey = PrimaryKey;
            index.Unique = Unique;
            foreach (IndexField indexField in IndexFields)
                index.IndexFields.Add(indexField.Clone());
            return index;
        }

        /// <summary>
        /// Compares whether the schema is identical to another instance.
        /// </summary>
        /// <param name="source">The source object to compare against.</param>
        public bool Compare(TableSchemaIndex source)
        {
            // Uniqueness differs, return false
            if (Unique != source.Unique) { return false; }
            // Index field count differs, return false
            if (IndexFields.Count != source.IndexFields.Count) { return false; }
            // Compare each index field schema
            foreach (IndexField indexField in IndexFields)
            {
                // Index field does not exist, return false
                if (!source.IndexFields.Contains(indexField.FieldName)) { return false; }
                // Sort direction differs, return false
                if (BackendInfo.DatabaseType == DatabaseType.SQLServer)
                {
                    if (indexField.SortDirection != source.IndexFields[indexField.FieldName].SortDirection) { return false; }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
