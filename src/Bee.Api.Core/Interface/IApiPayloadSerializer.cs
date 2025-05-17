using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// API 傳輸層 payload 專用序列化策略介面。
    /// </summary>
    public interface IApiPayloadSerializer
    {
        /// <summary>
        /// 將物件序列化為位元組陣列。
        /// </summary>
        /// <param name="value">要序列化的物件。</param>
        /// <param name="type">物件的型別。</param>
        byte[] Serialize(object value, Type type);

        /// <summary>
        /// 將位元組陣列反序列化為物件。
        /// </summary>
        /// <param name="bytes">要反序列化的位元組陣列。</param>
        /// <param name="type">反序列化後的物件型別。</param>
        object Deserialize(byte[] bytes, Type type);
    }
}
