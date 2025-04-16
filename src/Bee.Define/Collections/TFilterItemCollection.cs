using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 過濾條件集合。
    /// </summary>
    [Serializable]
    public class TFilterItemCollection : TCollectionBase<TFilterItem>
    {
        /// <summary>
        /// 加入條件。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="comparison">比較運算子。</param>
        /// <param name="value">過濾值。</param>
        public TFilterItem Add(string fieldName, EComparisonOperator comparison, string value)
        {
            TFilterItem oItem;

            oItem = new TFilterItem(fieldName, comparison, value);
            base.Add(oItem);
            return oItem;
        }
    }
}
