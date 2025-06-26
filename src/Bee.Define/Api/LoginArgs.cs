using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 登入的傳入引數。
    /// </summary>
    public class LoginArgs : BusinessArgs
    {
        /// <summary>
        /// 使用者帳號。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 密碼（明文或已加密）。
        /// </summary>
        public string Password { get; set; }
    }

}
