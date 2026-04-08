using Bee.Definition.Collections;
using MessagePack;
using System;
using System.Collections.Generic;

namespace Bee.Definition.Filters
{
    /// <summary>
    /// A collection of filter nodes.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class FilterNodeCollection : MessagePackCollectionBase<FilterNode>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FilterNodeCollection"/>.
        /// </summary>
        public FilterNodeCollection()
        { }

        /// <summary>
        /// Adds multiple <see cref="FilterNode"/> members to the collection.
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
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
