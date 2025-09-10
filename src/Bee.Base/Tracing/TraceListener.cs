using System;
using System.Runtime.CompilerServices;

namespace Bee.Base
{
    /// <summary>
    /// 預設執行流程監控器。
    /// </summary>
    public sealed class TraceListener : ITraceListener
    {
        private readonly TraceLayer _enabledLayers;
        private readonly ITraceWriter _writer;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="enabledLayers">要啟用的追蹤層級。</param>
        /// <param name="writer">用於輸出的追蹤寫入器。</param>
        public TraceListener(TraceLayer enabledLayers, ITraceWriter writer)
        {
            _enabledLayers = enabledLayers;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        /// <summary>
        /// 開始追蹤一個監控區段，回傳對應的 <see cref="TraceContext"/>。
        /// </summary>
        /// <param name="layer">所屬追蹤層級。</param>
        /// <param name="detail">額外描述，例如 SQL 語法或 API 路由。</param>
        /// <param name="name">監控名稱，例如方法名稱或事件名稱，若未設定自動帶入呼叫者方法名稱。</param>
        /// <returns>建立的追蹤上下文物件，若層級未啟用則為 null。</returns>
        public TraceContext TraceStart(TraceLayer layer, string detail = "", [CallerMemberName] string name = "")
        {
            if (!IsEnabled(layer)) return null;
            var ctx = new TraceContext(layer, name, detail);

            _writer.Write(new TraceEvent
            {
                Time = ctx.Start,
                Layer = ctx.Layer,
                Name = ctx.Name,
                Detail = detail ?? ctx.Detail,
                Kind = TraceEventKind.Start
            });

            return ctx;
        }

        /// <summary>
        /// 結束指定的追蹤區段，並輸出對應的 <see cref="TraceEvent"/>。
        /// </summary>
        /// <param name="ctx">開始追蹤時建立的上下文。</param>
        /// <param name="status">執行狀態，例如 Ok、Error 或 Cancelled。</param>
        /// <param name="detail">額外描述，可覆寫開始時的 Detail。</param>
        public void TraceEnd(TraceContext ctx, TraceStatus status = TraceStatus.Ok, string detail = null)
        {
            if (ctx == null) return;
            if (!IsEnabled(ctx.Layer)) return;
            ctx.Stopwatch.Stop();

            _writer.Write(new TraceEvent
            {
                Time = ctx.Start,
                Layer = ctx.Layer,
                Name = ctx.Name,
                Detail = detail ?? ctx.Detail,
                DurationMs = ctx.Stopwatch.Elapsed.TotalMilliseconds,
                Kind = TraceEventKind.End,
                Status = status
            });
        }

        /// <summary>
        /// 在任意位置寫入單點追蹤事件，不需成對呼叫。
        /// </summary>
        /// <param name="layer">所屬層級。</param>
        /// <param name="detail">事件描述。</param>
        /// <param name="name">監控名稱，例如方法名稱或事件名稱，若未設定自動帶入呼叫者方法名稱。</param>
        /// <param name="status">執行狀態。</param>
        public void TraceWrite(TraceLayer layer, string detail = "", [CallerMemberName] string name = "", TraceStatus status = TraceStatus.Ok)
        {
            if (!IsEnabled(layer)) return;

            _writer.Write(new TraceEvent
            {
                Time = DateTimeOffset.Now,
                Layer = layer,
                Name = name,
                Detail = detail,
                DurationMs = 0,
                Kind = TraceEventKind.Point,
                Status = status
            });
        }

        /// <summary>
        /// 判斷指定的追蹤層級是否已啟用。
        /// </summary>
        /// <param name="layer">要檢查的追蹤層級。</param>
        /// <returns>若該層級已啟用則回傳 true，否則回傳 false。</returns>
        private bool IsEnabled(TraceLayer layer)
        {
            return (_enabledLayers & layer) != TraceLayer.None;
        }
    }

}
