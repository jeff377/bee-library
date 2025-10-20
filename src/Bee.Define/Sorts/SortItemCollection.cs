using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 排序項目集合。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SortItemCollection : MessagePackCollectionBase<SortItem>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public SortItemCollection()
        { }
    }
}
