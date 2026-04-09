using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A table item in the database schema.
    /// </summary>
    [Serializable]
    [XmlType("TableItem")]
    [Description("Table item.")]
    [TreeNode]
    public class TableItem : KeyCollectionItem
    {
        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        [XmlAttribute]
        [Description("Table name.")]
        public string TableName
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
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{TableName} - {DisplayName}";
        }
    }
}
