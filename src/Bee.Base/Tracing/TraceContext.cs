using System;

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
        public TraceLayer Layer { get; }

        /// <summary>
        /// 追蹤名稱，例如方法名稱或事件名稱。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 額外描述，例如 SQL 語法或 API 路由。
        /// </summary>
        public string Detail { get; }

        /// <summary>
        /// 追蹤開始時間（本地時間）。
        /// </summary>
        public DateTimeOffset Start { get; }

        /// <summary>
        /// 用於計算執行耗時的計時器。
        /// </summary>
        public System.Diagnostics.Stopwatch Stopwatch { get; }

        /// <summary>
        /// 追蹤分類，可用於 Trace Viewer 依分類解析 Tag。
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// 追蹤物件，依 Category 解析內容。
        /// </summary>
        public object Tag { get; }

        /// <summary>
        /// 僅允許 <see cref="TraceListener"/> 建立 TraceContext。
        /// </summary>
        internal TraceContext(TraceLayer layer, string name, string detail, string category = "", object tag = null)
        {
            Layer = layer;
            Name = name ?? string.Empty;
            Detail = detail;
            Start = DateTimeOffset.Now; // 本地時間
            Stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Category = category ?? string.Empty;
            Tag = tag;
        }
    }


}
