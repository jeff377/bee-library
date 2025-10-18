using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 節點基底類別。
    /// </summary>
    [MessagePackObject]
    [Union(0, typeof(FilterCondition))]
    [Union(1, typeof(FilterGroup))]
    public abstract class FilterNode
    {
        /// <summary>
        /// 節點種類。
        /// </summary>
        [Key(0)]
        public abstract FilterNodeKind Kind { get; }
    }
}
