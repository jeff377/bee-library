using System;

namespace Bee.Base
{
    /// <summary>
    /// 表示一筆完整的追蹤事件，包含開始時間、耗時、狀態與描述，
    /// </summary>
    public sealed class TraceEvent
    {
        /// <summary>
        /// 事件發生時間（對於 TraceEnd 事件為開始時間）。
        /// </summary>
        public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// 所屬層級，例如 UI、API、Biz 或 Data。
        /// </summary>
        public TraceLayer Layer { get; set; }

        /// <summary>
        /// 事件名稱。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 事件描述，例如 SQL 語法、API 路由或業務動作摘要。
        /// </summary>
        public string Detail { get; set; } = string.Empty;

        /// <summary>
        /// 執行耗時（毫秒）。若為 TraceWrite 單點事件則為 0。
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// 追蹤事件的種類。
        /// </summary>
        public TraceEventKind Kind { get; set; }

        /// <summary>
        /// 執行狀態，例如 Ok、Error 或 Cancelled。
        /// </summary>
        public TraceStatus Status { get; set; } = TraceStatus.Ok;
    }

}
