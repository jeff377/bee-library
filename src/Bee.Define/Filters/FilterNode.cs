using MessagePack;
using System;
using System.Xml.Serialization;

namespace Bee.Define
{
    /// <summary>
    /// 節點基底類別。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    [Union(0, typeof(FilterCondition))]
    [Union(1, typeof(FilterGroup))]
    [XmlInclude(typeof(FilterCondition))]
    [XmlInclude(typeof(FilterGroup))]
    public abstract class FilterNode : MessagePackKeyCollectionItem
    {
        /// <summary>
        /// 節點種類。
        /// </summary>
        [Key(0)]
        public abstract FilterNodeKind Kind { get; }
    }
}
