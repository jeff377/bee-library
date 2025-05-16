using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 建立連線的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class TCreateSessionArgs : TBusinessArgs
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
}
