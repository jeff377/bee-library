using MessagePack;
using System.Collections.Generic;

namespace Bee.Define
{
    /// <summary>
    /// 單一欄位條件（例如 Name LIKE '%Lee%'、Age &gt; 18）。
    /// </summary>
    [MessagePackObject]
    public sealed class FilterCondition : FilterNode
    {
        /// <summary>
        /// 節點種類。
        /// </summary>
        public override FilterNodeKind Kind { get { return FilterNodeKind.Condition; } }

        /// <summary>
        /// 欄位名稱（邏輯欄位；需由後端白名單映射為實體欄位）。
        /// </summary>
        [Key(100)]
        public string Field { get; set; }

        /// <summary>
        /// 比較運算子。
        /// </summary>
        [Key(101)]
        public ComparisonOperator Operator { get; set; }

        /// <summary>
        /// 主要值（Equal、Like、&gt; 等）。
        /// </summary>
        [Key(102)]
        public object Value { get; set; }

        /// <summary>
        /// 第二值（Between 條件使用）。
        /// </summary>
        [Key(103)]
        public object SecondValue { get; set; }

        /// <summary>
        /// 值為 null 時是否忽略此條件。
        /// </summary>
        [Key(104)]
        public bool IgnoreIfNull { get; set; }

        /// <summary>
        /// 建立等於條件。
        /// </summary>
        public static FilterCondition Equal(string field, object value, bool ignoreIfNull = false)
        {
            return new FilterCondition { Field = field, Operator = ComparisonOperator.Equal, Value = value, IgnoreIfNull = ignoreIfNull };
        }

        /// <summary>
        /// 建立不等於條件。
        /// </summary>
        public static FilterCondition NotEqual(string field, object value, bool ignoreIfNull = false)
        {
            return new FilterCondition { Field = field, Operator = ComparisonOperator.NotEqual, Value = value, IgnoreIfNull = ignoreIfNull };
        }

        /// <summary>
        /// 建立 LIKE 條件（會自動加入萬用字元，請搭配 Contains/StartsWith/EndsWith）。
        /// </summary>
        public static FilterCondition Contains(string field, string keyword)
        {
            return new FilterCondition { Field = field, Operator = ComparisonOperator.Contains, Value = keyword };
        }

        /// <summary>
        /// 建立以指定前綴開頭的 LIKE 條件（相當於 SQL 的 LIKE 'value%'）。
        /// </summary>
        public static FilterCondition StartsWith(string field, string prefix)
        {
            return new FilterCondition { Field = field, Operator = ComparisonOperator.StartsWith, Value = prefix };
        }

        /// <summary>
        /// 建立以指定後綴結尾的 LIKE 條件（相當於 SQL 的 LIKE '%value'）。
        /// </summary>
        public static FilterCondition EndsWith(string field, string suffix)
        {
            return new FilterCondition { Field = field, Operator = ComparisonOperator.EndsWith, Value = suffix };
        }

        /// <summary>
        /// 建立 Between 條件。
        /// </summary>
        public static FilterCondition Between(string field, object from, object to, bool ignoreIfNull = false)
        {
            return new FilterCondition { Field = field, Operator = ComparisonOperator.Between, Value = from, SecondValue = to, IgnoreIfNull = ignoreIfNull };
        }

        /// <summary>
        /// 建立 IN 條件。
        /// </summary>
        public static FilterCondition In(string field, IEnumerable<object> values)
        {
            return new FilterCondition { Field = field, Operator = ComparisonOperator.In, Value = values };
        }
    }
}
