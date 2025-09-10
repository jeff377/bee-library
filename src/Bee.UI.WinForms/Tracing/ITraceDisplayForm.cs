using Bee.Base;

namespace Bee.UI.WinForms
{
    /// <summary>
    /// 定義可接收追蹤事件的表單介面。
    /// </summary>
    public interface ITraceDisplayForm
    {
        /// <summary>
        /// 顯示一筆追蹤事件。
        /// </summary>
        /// <param name="evt">追蹤事件內容。</param>
        void AppendTrace(TraceEvent evt);
    }
}
