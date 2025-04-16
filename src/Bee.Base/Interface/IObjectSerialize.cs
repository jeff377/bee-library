namespace Bee.Base
{
    /// <summary>
    /// 物件處理序列化介面。
    /// </summary>
    public interface IObjectSerialize : IObjectSerializeBase
    {
        /// <summary>
        /// 序列化狀態。
        /// </summary>
        ESerializeState SerializeState { get; }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        void SetSerializeState(ESerializeState serializeState);
    }
}
