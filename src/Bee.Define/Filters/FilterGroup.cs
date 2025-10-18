using MessagePack;
using System.Collections.Generic;

namespace Bee.Define
{
    /// <summary>
    /// 條件群組（以 AND/OR 串接多個節點）。
    /// </summary>
    [MessagePackObject]
    public sealed class FilterGroup : FilterNode
    {
        /// <summary>
        /// 節點種類。
        /// </summary>
        public override FilterNodeKind Kind { get { return FilterNodeKind.Group; } }

        /// <summary>
        /// 群組邏輯運算子。
        /// </summary>
        [Key(100)]
        public LogicalOperator Operator { get; set; }

        /// <summary>
        /// 子節點集合。
        /// </summary>
        [Key(101)]
        public List<FilterNode> Nodes { get; set; }

        /// <summary>
        /// 建構函式。
        /// </summary>
        public FilterGroup()
        {
            Nodes = new List<FilterNode>();
        }

        /// <summary>
        /// 建立 AND 群組。
        /// </summary>
        /// <param name="nodes">成員節點。</param>
        /// <returns>新的 <see cref="FilterGroup"/>。</returns>
        public static FilterGroup All(params FilterNode[] nodes)
        {
            var g = new FilterGroup();
            g.Operator = LogicalOperator.And;
            if (nodes != null) g.Nodes.AddRange(nodes);
            return g;
        }

        /// <summary>
        /// 建立 OR 群組。
        /// </summary>
        /// <param name="nodes">成員節點。</param>
        /// <returns>新的 <see cref="FilterGroup"/>。</returns>
        public static FilterGroup Any(params FilterNode[] nodes)
        {
            var g = new FilterGroup();
            g.Operator = LogicalOperator.Or;
            if (nodes != null) g.Nodes.AddRange(nodes);
            return g;
        }
    }
}
