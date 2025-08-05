using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Define
{
    /// <summary>
    /// 使用者資訊。
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// 使用者帳號。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 使用者名稱。
        /// </summary>
        public string UserName { get; set; }

        // 如需擴充：
        // public string Role { get; set; }
        // public string Department { get; set; }
    }

}
