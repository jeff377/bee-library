using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 過濾條件。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    [XmlType("FilterItem")]
    public class FilterItem : MessagePackCollectionItem
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public FilterItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="comparison">比較運算子。</param>
        /// <param name="value">過濾值。</param>
        public FilterItem(string fieldName, ComparisonOperator comparison, string value)
        {
            FieldName = fieldName;
            Comparison = comparison;
            Value = value;
        }

        #endregion

        /// <summary>
        /// 結合運算子。
        /// </summary>
        [XmlAttribute]
        [Key(100)]
        [Description("結合運算子。")]
        [DefaultValue(CombineOperator.And)]
        public CombineOperator Combine { get; set; } = CombineOperator.And;

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [XmlAttribute]
        [Key(101)]
        [Description("欄位名稱。")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 比較運算子。
        /// </summary>
        [XmlAttribute]
        [Key(102)]
        [Description("比較運算子。")]
        public ComparisonOperator Comparison {get; set; } = ComparisonOperator.Equal;

        /// <summary>
        /// 過濾值。
        /// </summary>
        [XmlAttribute]
        [Key(103)]
        [Description("過濾值。")]
        public string Value { get; set; } = string.Empty;
    }
}
