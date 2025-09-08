using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Base
{
    /// <summary>
    /// 表示一次追蹤區段的執行上下文，
    /// 在 <see cref="ITraceListener.TraceStart"/> 建立，
    /// 並於 <see cref="ITraceListener.TraceEnd"/> 使用以計算耗時與狀態。
    /// </summary>
    public sealed class TraceContext
    {
        /// <summary>
        /// 追蹤所屬的層級，例如 UI、API、Biz 或 Data。
        /// </summary>
        public TraceLayer Layer { get; set; }

        /// <summary>
        /// 追蹤名稱，例如方法名稱或事件名稱。
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 額外描述，例如 SQL 語法或 API 路由。
        /// </summary>
        public string Detail { get; set; }

        /// <summary>
        /// 追蹤開始時間。
        /// </summary>
        public DateTimeOffset Start { get; } = DateTimeOffset.Now;

        /// <summary>
        /// 用於計算執行耗時的計時器。
        /// </summary>
        public System.Diagnostics.Stopwatch Stopwatch { get; } = System.Diagnostics.Stopwatch.StartNew();
    }

}
