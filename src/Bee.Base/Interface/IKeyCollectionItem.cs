namespace Bee.Base
{
    /// <summary>
    /// 具鍵值的強型別集合成員介面。
    /// </summary>
    public interface IKeyCollectionItem
    {
        /// <summary>
        /// 鍵值。
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// 設定所屬集合。
        /// </summary>
        /// <param name="collection">集合。</param>
        void SetCollection(IKeyCollectionBase collection);

        /// <summary>
        /// 由集合中移除此成員。
        /// </summary>
        void Remove();
    }
}
