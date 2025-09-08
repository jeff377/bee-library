namespace Bee.Base
{
    /// <summary>
    /// 定義執行流程監控的介面，提供開始與結束追蹤的方法，
    /// 由應用程式在各層呼叫以建立 <see cref="TraceContext"/> 並輸出追蹤事件。
    /// </summary>
    public interface ITraceListener
    {
        /// <summary>
        /// 開始追蹤一個監控區段，回傳對應的 <see cref="TraceContext"/>。
        /// </summary>
        /// <param name="layer">所屬層級，例如 UI、API、Biz 或 Data。</param>
        /// <param name="name">監控名稱，例如方法名稱或事件名稱。</param>
        /// <param name="detail">額外描述，例如 SQL 語法或 API 路由。</param>
        /// <returns>建立的追蹤上下文物件。</returns>
        TraceContext TraceStart(TraceLayer layer, string name, string detail = null);

        /// <summary>
        /// 結束指定的追蹤區段，並輸出對應的 <see cref="TraceEvent"/>。
        /// </summary>
        /// <param name="ctx">開始追蹤時建立的上下文。</param>
        /// <param name="status">執行狀態，例如 Ok、Error 或 Cancelled。</param>
        /// <param name="detail">額外描述，可覆寫開始時的 Detail。</param>
        void TraceEnd(TraceContext ctx, TraceStatus status = TraceStatus.Ok, string detail = null);

        /// <summary>
        /// 在任意位置寫入單點追蹤事件，不需成對呼叫。
        /// </summary>
        /// <param name="layer">所屬層級。</param>
        /// <param name="name">事件名稱。</param>
        /// <param name="detail">事件描述。</param>
        /// <param name="status">執行狀態。</param>
        void TraceWrite(TraceLayer layer, string name, string detail = null, TraceStatus status = TraceStatus.Ok);
    }
}
