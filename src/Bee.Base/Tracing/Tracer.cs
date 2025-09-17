using System.Runtime.CompilerServices;

namespace Bee.Base
{
    /// <summary>
    /// 追蹤器靜態類別。
    /// </summary>
    public static class Tracer
    {
        /// <summary>
        /// 是否啟用追蹤。
        /// </summary>
        public static bool Enabled
            => SysInfo.TraceEnabled && SysInfo.TraceListener != null;

        /// <summary>
        /// 開始追蹤。
        /// </summary>
        /// <param name="layer">所屬層級，例如 UI、API、Biz 或 Data。</param>
        /// <param name="name">監控名稱，例如方法名稱或事件名稱。</param>
        /// <param name="detail">額外描述，例如 SQL 語法或 API 路由。</param>
        /// <param name="category">追蹤分類，可用於 Trace Viewer 依分類解析 Tag。</param>
        /// <param name="tag">追蹤物件，依 Category 解析內容。</param>
        /// <returns>建立的追蹤上下文物件。</returns>
        public static TraceContext Start(
            TraceLayer layer, string detail = "", [CallerMemberName] string name = "", 
            string category = "", object tag = null)
        {
            if (!Enabled) { return null; }
            return SysInfo.TraceListener.TraceStart(layer, detail, name, category, tag);
        }

        /// <summary>
        /// 結束追蹤。
        /// </summary>
        /// <param name="ctx">開始追蹤時建立的上下文。</param>
        /// <param name="status">執行狀態，例如 Ok、Error 或 Cancelled。</param>
        /// <param name="detail">額外描述，可覆寫開始時的 Detail。</param>
        public static void End(TraceContext ctx, TraceStatus status = TraceStatus.Ok, string detail = null)
        {
            if (!Enabled || ctx == null) return;
            SysInfo.TraceListener.TraceEnd(ctx, status, detail);
        }

        /// <summary>
        /// 單點追蹤。
        /// </summary>
        /// <param name="layer">所屬層級。</param>
        /// <param name="detail">事件描述。</param>
        /// <param name="name">監控名稱，例如方法名稱或事件名稱，若未設定自動帶入呼叫者方法名稱。</param>
        /// <param name="status">執行狀態。</param>
        /// <param name="category">追蹤分類，可用於 Trace Viewer 依分類解析 Tag。</param>
        /// <param name="tag">追蹤物件，依 Category 解析內容。</param>
        public static void Write(
            TraceLayer layer, string detail = "", [CallerMemberName] string name = "", TraceStatus status = TraceStatus.Ok,
            string category = "", object tag = null)
        {
            if (!Enabled) return;
            SysInfo.TraceListener.TraceWrite(layer, detail, name, status, category, tag);
        }
    }
}
