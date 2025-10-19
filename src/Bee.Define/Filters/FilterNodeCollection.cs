using MessagePack;
using System;
using System.Collections.Generic;

namespace Bee.Define
{
    /// <summary>
    /// 過濾節點集合。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class FilterNodeCollection : MessagePackKeyCollectionBase<FilterNode>
    {
        /// <summary>
        /// 批次加入多個 <see cref="FilterNode"/> 成員。
        /// </summary>
        /// <param name="nodes">要加入的節點集合。</param>
        public void AddRange(IEnumerable<FilterNode> nodes)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node != null)
                    this.Add(node);
            }
        }
    }
}
