using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 取得 API 傳輸層的 Payload 編碼選項的傳出結果。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class TGetApiPayloadOptionsResult : TBusinessResult
    {
        /// <summary>
        /// 使用的序列化格式（例如 messagepack、binaryformatter）。
        /// </summary>
        [Key(100)]
        public string Serializer { get; set; }

        /// <summary>
        /// 使用的壓縮格式（例如 gzip、none）。
        /// </summary>
        [Key(101)]
        public string Compressor { get; set; }

        /// <summary>
        /// 使用的加密方式（例如 aes256、none）。
        /// </summary>
        [Key(102)]
        public string Encryptor { get; set; }
    }

}
