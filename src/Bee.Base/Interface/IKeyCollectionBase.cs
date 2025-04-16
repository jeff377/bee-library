namespace Bee.Base
{
    /// <summary>
    /// 具鍵值的強型別集合介面。
    /// </summary>
    public interface IKeyCollectionBase
    {
        /// <summary>
        /// 擁有者。
        /// </summary>
        object Owner { get; }

        /// <summary>
        /// 變更成員鍵值。
        /// </summary>
        /// <param name="key">鍵值。</param>
        /// <param name="value">成員。</param>
        void ChangeItemKey(string key, IKeyCollectionItem value);

        /// <summary>
        /// 移除成員。
        /// </summary>
        /// <param name="value">成員。</param>
        void Remove(IKeyCollectionItem value);

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="value">成員。</param>
        void Add(IKeyCollectionItem value);

        /// <summary>
        /// 插入成員。
        /// </summary>
        /// <param name="index">索引位置。</param>
        /// <param name="value">成員。</param>
        void Insert(int index, IKeyCollectionItem value);
    }
}
