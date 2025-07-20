using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 提供 API Payload 處理相關選項，例如序列化、壓縮與加密。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    [XmlType("ApiPayloadOptions")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Description("提供 API Payload 處理相關選項，例如序列化、壓縮與加密。")]
    public class ApiPayloadOptions
    {
        /// <summary>
        /// 指定序列化器名稱，例如：messagepack、binaryformatter。
        /// </summary>
        [Key(0)]
        [Description("指定序列化器名稱，例如：messagepack、binaryformatter。")]
        public string Serializer { get; set; } = "binaryformatter";

        /// <summary>
        /// 指定壓縮器名稱，例如：gzip、none。
        /// </summary>
        [Key(1)]
        [Description("指定壓縮器名稱，例如：gzip、none。")]
        public string Compressor { get; set; } = "gzip";

        /// <summary>
        /// 指定加密器名稱，例如：aes-cbc-hmac、none。
        /// </summary>
        [Key(2)]
        [Description("指定加密器名稱，例如：aes-cbc-hmac、none。")]
        public string Encryptor { get; set; } = "aes-cbc-hmac";

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"Serializer: {Serializer}, Compressor: {Compressor}, Encryptor: {Encryptor}";
        }
    }

}
