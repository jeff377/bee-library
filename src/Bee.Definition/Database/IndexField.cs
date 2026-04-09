using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Database
{
    /// <summary>
    /// An index field.
    /// </summary>
    [Serializable]
    [XmlType("IndexField")]
    [Description("Index field.")]
    public class IndexField : KeyCollectionItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IndexField"/>.
        /// </summary>
        public IndexField()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="IndexField"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="sortDirection">The sort direction.</param>
        public IndexField(string fieldName, SortDirection sortDirection)
        {
            FieldName = fieldName;
            SortDirection = sortDirection;
        }

        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Field name.")]
        public string FieldName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Sort direction.")]
        [DefaultValue(SortDirection.Asc)]
        public SortDirection SortDirection { get; set; } = SortDirection.Asc;

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        public IndexField Clone()
        {
            var indexField = new IndexField();
            indexField.FieldName = FieldName;
            indexField.SortDirection = SortDirection;
            return indexField;
        }
    }
}
