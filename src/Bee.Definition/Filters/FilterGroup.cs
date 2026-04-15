using System;
using System.Xml.Serialization;
using MessagePack;
using System.Text.Json.Serialization;

namespace Bee.Definition.Filters
{
    /// <summary>
    /// A filter condition group that combines multiple nodes with AND/OR logic.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public sealed class FilterGroup : FilterNode
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FilterGroup"/>.
        /// </summary>
        public FilterGroup()
        {
            Nodes = new FilterNodeCollection();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FilterGroup"/>.
        /// </summary>
        /// <param name="operator">The logical operator for the group.</param>
        public FilterGroup(LogicalOperator @operator)
        {
            Operator = @operator;
            Nodes = new FilterNodeCollection();
        }

        /// <summary>
        /// Gets the node kind.
        /// </summary>
        public override FilterNodeKind Kind { get { return FilterNodeKind.Group; } }

        /// <summary>
        /// Gets or sets the logical operator for this group.
        /// </summary>
        [Key(100)]
        public LogicalOperator Operator { get; set; }

        /// <summary>
        /// Gets or sets the child node collection.
        /// </summary>
        [Key(101)]
        [XmlArrayItem(typeof(FilterCondition))]
        [XmlArrayItem(typeof(FilterGroup))]
        [JsonConverter(typeof(FilterNodeCollectionJsonConverter))]
        public FilterNodeCollection Nodes { get; set; }

        /// <summary>
        /// Determines whether the Nodes property should be serialized.
        /// </summary>
        /// <returns>
        /// XmlSerializer automatically detects ShouldSerialize[PropertyName]() methods; if the method returns false, the property is not serialized.
        /// </returns>
        public bool ShouldSerializeNodes()
        {
            return Nodes != null && Nodes.Count > 0;
        }

        /// <summary>
        /// Creates an AND group.
        /// </summary>
        /// <param name="nodes">The member nodes.</param>
        public static FilterGroup All(params FilterNode[] nodes)
        {
            var g = new FilterGroup();
            g.Operator = LogicalOperator.And;
            if (nodes != null) g.Nodes.AddRange(nodes);
            return g;
        }

        /// <summary>
        /// Creates an OR group.
        /// </summary>
        /// <param name="nodes">The member nodes.</param>
        public static FilterGroup Any(params FilterNode[] nodes)
        {
            var g = new FilterGroup();
            g.Operator = LogicalOperator.Or;
            if (nodes != null) g.Nodes.AddRange(nodes);
            return g;
        }
    }
}
