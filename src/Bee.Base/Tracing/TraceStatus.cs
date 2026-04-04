namespace Bee.Base.Tracing
{
    /// <summary>
    /// 追蹤事件的執行狀態，用於標示該次追蹤的結果。
    /// </summary>
    public enum TraceStatus
    {
        /// <summary>
        /// 成功完成，無錯誤或中斷。
        /// </summary>
        Ok,
        /// <summary>
        /// 發生錯誤，例如例外或執行失敗。
        /// </summary>
        Error,
        /// <summary>
        /// 執行被取消，例如使用者中斷或系統中止。
        /// </summary>
        Cancelled
    }
}
