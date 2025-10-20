using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 索引欄位。
    /// </summary>
    [Serializable]
    [XmlType("IndexField")]
    [Description("索引欄位。")]
    public class IndexField : KeyCollectionItem
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public IndexField()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="sortDirection">排序方式。</param>
        public IndexField(string fieldName, SortDirection sortDirection)
        {
            FieldName = fieldName;
            SortDirection = sortDirection;
        }

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("欄位名稱。")]
        public string FieldName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 排序方式。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("排序方式。")]
        [DefaultValue(SortDirection.Asc)]
        public SortDirection SortDirection { get; set; } = SortDirection.Asc;

        /// <summary>
        /// 建立複本。
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
