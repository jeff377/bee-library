using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 過濾條件。
    /// </summary>
    [Serializable]
    [XmlType("FilterItem")]
    public class TFilterItem : TCollectionItem
    {
        private ECombineOperator _Combine = ECombineOperator.And;
        private string _FieldName = string.Empty;
        private EComparisonOperator _Comparison = EComparisonOperator.Equal;
        private string _Value = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TFilterItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="comparison">比較運算子。</param>
        /// <param name="value">過濾值。</param>
        public TFilterItem(string fieldName, EComparisonOperator comparison, string value)
        {
            _FieldName = fieldName;
            _Comparison = comparison;
            _Value = value;
        }

        #endregion

        /// <summary>
        /// 結合運算子。
        /// </summary>
        [XmlAttribute]
        [Description("結合運算子。")]
        [DefaultValue(ECombineOperator.And)]
        public ECombineOperator Combine
        {
            get { return _Combine; }
            set { _Combine = value; }
        }

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [XmlAttribute]
        [Description("欄位名稱。")]
        public string FieldName
        {
            get { return _FieldName; }
            set { _FieldName = value; }
        }

        /// <summary>
        /// 比較運算子。
        /// </summary>
        [XmlAttribute]
        [Description("比較運算子。")]
        public EComparisonOperator Comparison
        {
            get { return _Comparison; }
            set { _Comparison = value; }
        }

        /// <summary>
        /// 過濾值。
        /// </summary>
        [XmlAttribute]
        [Description("過濾值。")]
        public string Value
        {
            get { return _Value; }
            set { _Value = value; }
        }
    }
}
