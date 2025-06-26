using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Define
{
    /// <summary>
    ///  登入的傳出結果。
    /// </summary>
    public class LoginResult : BusinessResult
    {
        /// <summary>
        /// 存取令牌（AccessToken），後續呼叫 API 用於身份驗證。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 登入成功後 AccessToken 的有效期限（UTC 時間）。
        /// 用戶端應在此時間之前完成所有受保護的 API 呼叫。
        /// </summary>
        public DateTime ExpiredAt { get; set; }
    }

}


