using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 過濾條件集合。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class FilterItemCollection : MessagePackCollectionBase<FilterItem>
    {
        /// <summary>
        /// 加入條件。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="comparison">比較運算子。</param>
        /// <param name="value">過濾值。</param>
        public FilterItem Add(string fieldName, ComparisonOperator comparison, string value)
        {
            FilterItem oItem;

            oItem = new FilterItem(fieldName, comparison, value);
            base.Add(oItem);
            return oItem;
        }
    }
}
