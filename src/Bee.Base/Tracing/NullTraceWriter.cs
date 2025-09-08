namespace Bee.Base
{
    /// <summary>
    /// Null 物件模式的追蹤寫入器，
    /// 呼叫時不做任何動作，用於停用追蹤或測試環境。
    /// </summary>
    public sealed class NullTraceWriter : ITraceWriter
    {
        /// <summary>
        /// 將指定的追蹤事件輸出至目的地。
        /// </summary>
        /// <param name="evt">要輸出的追蹤事件。</param>
        public void Write(TraceEvent evt)
        {
            // Do nothing.
        }
    }

}
