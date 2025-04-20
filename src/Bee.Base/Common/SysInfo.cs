namespace Bee.Base
{
    /// <summary>
    /// 系統資訊，前端及後端通用的參數及環境設置。
    /// </summary>
    public static class SysInfo
    {
        private static string _Version = string.Empty;

        /// <summary>
        /// 系統主版琥。
        /// </summary>
        public static string Version
        {
            get { return _Version; }
            set { _Version = value; }
        }
    }
}
