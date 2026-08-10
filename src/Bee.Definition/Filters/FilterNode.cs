using Bee.Base.Collections;
using System.Xml.Serialization;

namespace Bee.Definition.Filters
{
    /// <summary>
    /// Abstract base class for filter nodes.
    /// </summary>
    [XmlInclude(typeof(FilterCondition))]
    [XmlInclude(typeof(FilterGroup))]
    public abstract class FilterNode : CollectionItem
    {
        /// <summary>
        /// Gets the node kind.
        /// </summary>
        /// <remarks>
        /// This is the polymorphic discriminator on both wire formats, and both read it from
        /// outside the type: <c>FilterNodeCollectionJsonConverter</c> for JSON, and the API layer's
        /// filter node formatter for MessagePack. Being get-only, it is never bound back on the way
        /// in — each subclass computes it.
        /// <para>
        /// WARNING: Do NOT add <c>[JsonIgnore]</c> here for "tri-format consistency". Ignoring it in
        /// JSON would silently deserialize every group as a condition.
        /// </para>
        /// </remarks>
        public abstract FilterNodeKind Kind { get; }
    }
}
