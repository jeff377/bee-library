using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 建立連線的傳出結果。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class CreateSessionResult : BusinessResult
    {
        /// <summary>
        /// 存取令牌（AccessToken），後續呼叫 API 用於身份驗證。
        /// </summary>
        [Key(100)]
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 建立連線成功後 AccessToken 的有效期限（UTC 時間）。
        /// 用戶端應在此時間之前完成所有受保護的 API 呼叫。
        /// </summary>
        [Key(101)]
        public DateTime ExpiredAt { get; set; }
    }
}
