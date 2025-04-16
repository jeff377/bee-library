using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 索引欄位集合。
    /// </summary>
    [Serializable]
    public class TIndexFieldCollection : TKeyCollectionBase<TIndexField>
    {
        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="sortDirection">排序方式。</param>
        public TIndexField Add(string fieldName, ESortDirection sortDirection = ESortDirection.Asc)
        {
            TIndexField oItem;

            oItem = new TIndexField(fieldName, sortDirection);
            this.Add(oItem);
            return oItem;
        }
    }
}
