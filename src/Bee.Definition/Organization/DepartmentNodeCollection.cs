using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition.Organization
{
    /// <summary>
    /// A collection of <see cref="DepartmentNode"/>. Tri-modal serialisable (XML / JSON /
    /// MessagePack) via <see cref="MessagePackCollectionBase{T}"/>.
    /// </summary>
    [MessagePackObject]
    public class DepartmentNodeCollection : MessagePackCollectionBase<DepartmentNode>
    {
        /// <summary>
        /// Initializes a new empty <see cref="DepartmentNodeCollection"/>.
        /// </summary>
        public DepartmentNodeCollection() { }

        /// <summary>
        /// Adds multiple <see cref="DepartmentNode"/> members to the collection.
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
        public void AddRange(IEnumerable<DepartmentNode> nodes)
        {
            if (nodes == null) { return; }
            foreach (var node in nodes.Where(n => n != null))
            {
                this.Add(node);
            }
        }
    }
}
