using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Api.Core
{
    /// <summary>
    /// 不執行任何加密或解密操作的加密器實作。
    /// </summary>
    public class NoEncryptionEncryptor : IApiPayloadEncryptor
    {
        /// <summary>
        /// 加密演算法的識別字串, ，none 表示不進行加密。
        /// </summary>
        public string EncryptionMethod => "none";

        /// <summary>
        /// 傳回原始資料，未進行加密。
        /// </summary>
        public byte[] Encrypt(byte[] bytes)
        {
            return bytes;
        }

        /// <summary>
        /// 傳回原始資料，未進行解密。
        /// </summary>
        public byte[] Decrypt(byte[] bytes)
        {
            return bytes;
        }
    }
}
