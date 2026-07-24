using Bee.Definition.Collections;
using MessagePack;
using System.Xml.Serialization;

namespace Bee.Definition.Filters
{
    /// <summary>
    /// Abstract base class for filter nodes.
    /// </summary>
    [MessagePackObject]
    [Union(0, typeof(FilterCondition))]
    [Union(1, typeof(FilterGroup))]
    [XmlInclude(typeof(FilterCondition))]
    [XmlInclude(typeof(FilterGroup))]
    public abstract class FilterNode : MessagePackCollectionItem
    {
        /// <summary>
        /// Gets the node kind.
        /// </summary>
        /// <remarks>
        /// Not carried over the MessagePack wire: the concrete node type is already resolved by the
        /// <c>[Union]</c> tag, and this property is a get-only discriminator that each subclass
        /// computes, so serializing it would only add redundant bytes that cannot be restored.
        /// </remarks>
        [IgnoreMember]
        public abstract FilterNodeKind Kind { get; }
    }
}
