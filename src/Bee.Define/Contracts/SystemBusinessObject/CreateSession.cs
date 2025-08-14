using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 建立連線的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class CreateSessionArgs : BusinessArgs
    {
        /// <summary>
        /// 用戶帳號。
        /// </summary>
        [Key(100)]
        public string UserID { get; set; } = string.Empty;

        /// <summary>
        /// 到期秒數，預設 3600 秒。
        /// </summary>
        [Key(101)]
        public int ExpiresIn { get; set; } = 3600;

        /// <summary>
        /// 一次性有效。
        /// </summary>
        [Key(102)]
        public bool OneTime { get; set; } = false;
    }

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
