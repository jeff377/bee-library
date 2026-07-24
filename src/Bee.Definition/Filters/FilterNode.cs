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
        /// <para>
        /// WARNING: Do NOT add <c>[JsonIgnore]</c> here for "tri-format consistency". On the JSON
        /// wire this property IS the polymorphic discriminator — <c>FilterNodeCollectionJsonConverter</c>
        /// reads the <c>kind</c> property to choose <see cref="FilterCondition"/> vs
        /// <see cref="FilterGroup"/>. Ignoring it in JSON would silently deserialize every group as a
        /// condition. The asymmetry (ignored for MessagePack, kept for JSON) is intentional.
        /// </para>
        /// </remarks>
        [IgnoreMember]
        public abstract FilterNodeKind Kind { get; }
    }
}
