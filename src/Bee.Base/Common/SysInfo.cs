namespace Bee.Base
{
    /// <summary>
    /// 系統資訊，前端及後端通用的參數及環境設置。
    /// </summary>
    public static class SysInfo
    {
        /// <summary>
        /// 系統主版琥。
        /// </summary>
        public static string Version { get; set; } = string.Empty;

        /// <summary>
        /// 是否為偵錯模式。
        /// </summary>
        public static bool IsDebugMode { get; set; } = false;
    }
}
