namespace Bee.Base.BackgroundServices
{
    /// <summary>
    /// 背景服務執行動作。
    /// </summary>
    public enum BackgroundServiceAction
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        Initialize,
        /// <summary>
        /// 啟動。
        /// </summary>
        Start,
        /// <summary>
        /// 執行。
        /// </summary>
        Run,
        /// <summary>
        /// 停止。
        /// </summary>
        Stop
    }
}
