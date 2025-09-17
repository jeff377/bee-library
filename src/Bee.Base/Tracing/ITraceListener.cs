using System.Runtime.CompilerServices;

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
        /// <param name="layer">所屬追蹤層級。</param>
        /// <param name="detail">額外描述，例如 SQL 語法或 API 路由。</param>
        /// <param name="name">監控名稱，例如方法名稱或事件名稱，若未設定自動帶入呼叫者方法名稱。</param>
        /// <param name="category">追蹤分類，可用於 Trace Viewer 依分類解析 Tag。</param>
        /// <param name="tag">追蹤物件，依 Category 解析內容。</param>
        TraceContext TraceStart(
            TraceLayer layer, string detail = "", [CallerMemberName] string name = "",
            string category = "", object tag = null);

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
        /// <param name="detail">事件描述。</param>
        /// <param name="name">監控名稱，例如方法名稱或事件名稱，若未設定自動帶入呼叫者方法名稱。</param>
        /// <param name="status">執行狀態。</param>
        /// <param name="category">追蹤分類，可用於 Trace Viewer 依分類解析 Tag。</param>
        /// <param name="tag">追蹤物件，依 Category 解析內容。</param>
        void TraceWrite(
            TraceLayer layer, string detail = "", [CallerMemberName] string name = "", TraceStatus status = TraceStatus.Ok,
            string category = "", object tag = null);
    }
}
