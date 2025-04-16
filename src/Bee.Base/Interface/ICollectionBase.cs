namespace Bee.Base
{
    /// <summary>
    /// 強型別集合介面。
    /// </summary>
    public interface ICollectionBase
    {
        /// <summary>
        /// 擁有者。
        /// </summary>
        object Owner { get; }

        /// <summary>
        /// 移除成員。
        /// </summary>
        /// <param name="value">成員。</param>
        void Remove(ICollectionItem value);

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="value">成員。</param>
        void Add(ICollectionItem value);

        /// <summary>
        /// 插入成員。
        /// </summary>
        /// <param name="index">索引位置。</param>
        /// <param name="value">成員。</param>
        void Insert(int index, ICollectionItem value);
    }
}
