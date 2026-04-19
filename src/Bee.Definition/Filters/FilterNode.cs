using Bee.Definition.Collections;
using MessagePack;
using System.Xml.Serialization;

namespace Bee.Definition.Filters
{
    /// <summary>
    /// Abstract base class for filter nodes.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    [Union(0, typeof(FilterCondition))]
    [Union(1, typeof(FilterGroup))]
    [XmlInclude(typeof(FilterCondition))]
    [XmlInclude(typeof(FilterGroup))]
    public abstract class FilterNode : MessagePackCollectionItem
    {
        /// <summary>
        /// Gets the node kind.
        /// </summary>
        [Key(10)]
        public abstract FilterNodeKind Kind { get; }
    }
}
