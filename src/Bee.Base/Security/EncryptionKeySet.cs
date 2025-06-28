using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// 加密金鑰組的抽象基底類別。
    /// </summary>
    public abstract class EncryptionKeySet
    {
        /// <summary>
        /// 加密演算法代碼（如：AES-HMAC, AES-GCM, ChaCha20）。
        /// </summary>
        public abstract string Algorithm { get; }
    }

}
