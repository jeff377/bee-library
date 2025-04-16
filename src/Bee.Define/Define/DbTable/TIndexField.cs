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
    public class TIndexField : TKeyCollectionItem
    {
        private ESortDirection _SortDirection = ESortDirection.Asc;

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TIndexField()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="sortDirection">排序方式。</param>
        public TIndexField(string fieldName, ESortDirection sortDirection)
        {
            this.FieldName = fieldName;
            _SortDirection = sortDirection;
        }

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [XmlAttribute]
        [Category(Category.Data)]
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
        [Category(Category.Data)]
        [Description("排序方式。")]
        [DefaultValue(ESortDirection.Asc)]
        public ESortDirection SortDirection
        {
            get { return this._SortDirection; }
            set { this._SortDirection = value; }
        }

        /// <summary>
        /// 建立複本。
        /// </summary>
        public TIndexField Clone()
        {
            TIndexField oIndexField;

            oIndexField = new TIndexField();
            oIndexField.FieldName = this.FieldName;
            oIndexField.SortDirection = this.SortDirection;
            return oIndexField;
        }
    }
}
