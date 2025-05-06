using System;

namespace Bee.Define
{
    /// <summary>
    /// Ping 方法的傳回結果。
    /// </summary>
    [Serializable]
    public class TPingResult : TBusinessResult
    {
        /// <summary>
        /// 狀態，通常為 "ok" 或 "pong"。
        /// </summary>
        public string Status { get; set; } = "ok";

        /// <summary>
        /// 伺服器當下的 UTC 時間。
        /// </summary>
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 可選的版本資訊。
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 回傳追蹤 ID（如有）。
        /// </summary>
        public string TraceId { get; set; }
    }

}
