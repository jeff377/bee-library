using System;

namespace Bee.Define
{
    /// <summary>
    /// 建立連線的傳出結果。
    /// </summary>
    [Serializable]
    public class TCreateSessionResult : TBusinessResult
    {
        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 到期時間。
        /// </summary>
        public DateTime Expires { get; set; } = DateTime.MinValue;
    }
}
