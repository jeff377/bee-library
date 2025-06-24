using System;
using Bee.Define;

namespace Bee.Api.Core
{
    /// <summary>
    /// 使用 MessagePack 的 API Payload 序列化器。
    /// </summary>
    public class MessagePackPayloadSerializer : IApiPayloadSerializer
    {
        /// <summary>
        /// 序列化格式的識別字串。
        /// </summary>
        public string SerializationMethod => "messagepack";

        /// <summary>
        /// 將物件序列化為位元組陣列。
        /// </summary>
        /// <param name="value">要序列化的物件。</param>
        /// <param name="type">物件的型別。</param>
        public byte[] Serialize(object value, Type type)
        {
            return MessagePackHelper.Serialize(value, type);
        }

        /// <summary>
        /// 將位元組陣列反序列化為物件。
        /// </summary>
        /// <param name="bytes">要反序列化的位元組陣列。</param>
        /// <param name="type">反序列化後的物件型別。</param>
        public object Deserialize(byte[] bytes, Type type)
        {
            return MessagePackHelper.Deserialize(bytes, type);
        }
    }

}
