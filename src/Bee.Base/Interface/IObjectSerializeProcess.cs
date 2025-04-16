namespace Bee.Base
{
    /// <summary>
    /// 物件序列化處理程序介面。
    /// </summary>
    public interface IObjectSerializeProcess
    {
        /// <summary>
        /// 執行序列化前的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        void BeforeSerialize(ESerializeFormat serializeFormat);

        /// <summary>
        /// 執行序列化後的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        void AfterSerialize(ESerializeFormat serializeFormat);

        /// <summary>
        /// 執行反序列化後的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        void AfterDeserialize(ESerializeFormat serializeFormat);
    }
}
