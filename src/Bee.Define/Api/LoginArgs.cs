using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 登入的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class LoginArgs : BusinessArgs
    {
        /// <summary>
        /// 使用者帳號。
        /// </summary>
        [Key(100)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 密碼（明文或已加密）。
        /// </summary>
        [Key(101)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 用戶端加密公鑰。
        /// </summary>
        [Key(102)]
        public string ClientPublicKey { get; set; } = string.Empty;
    }

}
