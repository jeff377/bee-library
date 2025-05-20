using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define
{
    /// <summary>
    /// 提供 API Payload 處理相關選項，例如序列化、壓縮與加密。
    /// </summary>
    [Serializable]
    [XmlType("ApiPayloadOptions")]
    [Description("提供 API Payload 處理相關選項，例如序列化、壓縮與加密。")]
    public class TApiPayloadOptions
    {
        /// <summary>
        /// 指定序列化器名稱，例如：messagepack、binaryformatter。
        /// </summary>
        [Description("指定序列化器名稱，例如：messagepack、binaryformatter。")]
        public string Serializer { get; set; } = "messagepack";

        /// <summary>
        /// 指定壓縮器名稱，例如：gzip、none。
        /// </summary>
        [Description("指定壓縮器名稱，例如：gzip、none。")]
        public string Compressor { get; set; } = "gzip";

        /// <summary>
        /// 指定加密器名稱，例如：aes256、none。
        /// </summary>
        [Description("指定加密器名稱，例如：aes256、none。")]
        public string Encryptor { get; set; } = "aes256";
    }

}
