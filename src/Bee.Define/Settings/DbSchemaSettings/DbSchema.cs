using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Bee.Base.Collections;

namespace Bee.Define.Settings
{
    /// <summary>
    /// A database schema definition.
    /// </summary>
    [Serializable]
    [XmlType("DbSchema")]
    [Description("Database schema.")]
    [TreeNode]
    public class DbSchema : KeyCollectionItem
    {
        private TableItemCollection _tables = null;

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        [XmlAttribute]
        [Description("Database name.")]
        public string DbName
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the table collection.
        /// </summary>
        [Description("Table collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        public TableItemCollection Tables
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _tables)) { return null; }
                if (_tables == null) { _tables = new TableItemCollection(this); }
                return _tables;
            }
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_tables, serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{DbName} - {DisplayName}";
        }
    }
}
