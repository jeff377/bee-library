namespace Bee.Base
{
    /// <summary>
    /// 強型別集合成員介面。
    /// </summary>
    public interface ICollectionItem
    {
        /// <summary>
        /// 設定所屬集合。
        /// </summary>
        /// <param name="collection">集合。</param>
        void SetCollection(ICollectionBase collection);

        /// <summary>
        /// 由集合中移除此成員。
        /// </summary>
        void Remove();
    }
}
