using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 排序欄位集合。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SortFieldCollection : MessagePackCollectionBase<SortField>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public SortFieldCollection()
        { }
    }
}
