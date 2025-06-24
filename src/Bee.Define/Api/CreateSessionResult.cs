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
        /// 存取令牌。
        /// </summary>
        [Key(100)]
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 到期時間。
        /// </summary>
        [Key(101)]
        public DateTime Expires { get; set; } = DateTime.MinValue;
    }
}
