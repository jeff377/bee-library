namespace Bee.Base
{
    /// <summary>
    /// 用於輸出的追蹤寫入介面，負責將 <see cref="TraceEvent"/> 寫出至不同目的地，
    /// 例如：WinForms 介面、檔案、Console 或外部監控系統。
    /// </summary>
    public interface ITraceWriter
    {
        /// <summary>
        /// 將指定的追蹤事件輸出至目的地。
        /// </summary>
        /// <param name="evt">要輸出的追蹤事件。</param>
        void Write(TraceEvent evt);
    }

}
