namespace Bee.Base.Tracing
{
    /// <summary>
    /// 追蹤事件的種類，用於區分開始、結束與單點事件。
    /// </summary>
    public enum TraceEventKind
    {
        /// <summary>
        /// 代表一個追蹤區段的開始事件，
        /// 通常由 <see cref="ITraceListener.TraceStart"/> 產生。
        /// </summary>
        Start = 0,
        /// <summary>
        /// 代表一個追蹤區段的結束事件，
        /// 通常由 <see cref="ITraceListener.TraceEnd"/> 產生，
        /// 並包含該區段的耗時與執行狀態。
        /// </summary>
        End = 1,
        /// <summary>
        /// 代表一個單點事件，不需要成對呼叫，
        /// 通常由 <see cref="ITraceListener.TraceWrite"/> 產生。
        /// </summary>
        Point = 2
    }
}
