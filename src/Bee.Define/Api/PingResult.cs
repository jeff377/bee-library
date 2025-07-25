﻿using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// Ping 方法的傳回結果。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class PingResult : BusinessResult
    {
        /// <summary>
        /// 狀態，通常為 "ok" 或 "pong"。
        /// </summary>
        [Key(100)]
        public string Status { get; set; } = "ok";

        /// <summary>
        /// 伺服器當下的 UTC 時間。
        /// </summary>
        [Key(101)]
        public DateTime ServerTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 可選的版本資訊。
        /// </summary>
        [Key(102)]
        public string Version { get; set; }

        /// <summary>
        /// 回傳追蹤 ID（如有）。
        /// </summary>
        [Key(103)]
        public string TraceId { get; set; }
    }

}
