using Bee.Base;
using Bee.Base.Collections;
using System;

namespace Bee.Define.Database
{
    /// <summary>
    /// 索引欄位集合。
    /// </summary>
    [Serializable]
    public class IndexFieldCollection : KeyCollectionBase<IndexField>
    {
        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="sortDirection">排序方式。</param>
        public IndexField Add(string fieldName, SortDirection sortDirection = SortDirection.Asc)
        {
            var indexFIeld = new IndexField(fieldName, sortDirection);
            this.Add(indexFIeld);
            return indexFIeld;
        }
    }
}
