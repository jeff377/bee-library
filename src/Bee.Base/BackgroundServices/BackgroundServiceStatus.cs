namespace Bee.Base.BackgroundServices
{
    /// <summary>
    /// 背景服務狀態。
    /// </summary>
    public enum BackgroundServiceStatus
    {
        /// <summary>
        /// 停止。
        /// </summary>
        Stopped,
        /// <summary>
        /// 正在啟動。
        /// </summary>
        StartPending,
        /// <summary>
        /// 正在停止。
        /// </summary>
        StopPending,
        /// <summary>
        /// 執行中。
        /// </summary>
        Running
    }
}
