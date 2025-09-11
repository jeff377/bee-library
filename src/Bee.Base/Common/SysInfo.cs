using System.Collections.Generic;

namespace Bee.Base
{
    /// <summary>
    /// 系統資訊，前端及後端通用的參數及環境設置。
    /// </summary>
    public static class SysInfo
    {
        static SysInfo()
        {
            // 預設加入允許 JSON-RPC 傳遞資料的型別命名空間
            AllowedTypeNamespaces = new List<string> { "Bee.Base", "Bee.Define" };
        }

        /// <summary>
        /// 系統主版號。
        /// </summary>
        public static string Version { get; set; } = string.Empty;

        /// <summary>
        /// 日誌寫入器。
        /// </summary>
        public static ILogWriter LogWriter { get; set; } = new NullLogWriter();

        /// <summary>
        /// 記錄選項，用於設定日誌記錄的相關參數。
        /// </summary>
        public static LogOptions LogOptions { get; set; } = new LogOptions();

        /// <summary>
        /// 是否啟用追蹤（唯讀，當 TraceListener 不為 null 時啟用）。
        /// </summary>
        public static bool TraceEnabled => TraceListener != null;

        /// <summary>
        /// 執行流程監控器，提供系統層級的追蹤區段監控功能，
        /// 由應用程式呼叫以記錄執行流程的開始、結束與單點事件，
        /// 便於效能分析與異常追蹤。
        /// </summary>
        public static ITraceListener TraceListener { get; set; } = null;

        /// <summary>
        /// 是否為偵錯模式。
        /// </summary>
        public static bool IsDebugMode { get; set; } = false;

        /// <summary>
        /// 是否為工具程式模式（例如 SettingsEditor.exe）。
        /// 此屬性只能由程式啟動階段指定，不允許從設定檔載入。
        /// 用於允許近端執行且不需 AccessToken 的驗證流程。
        /// </summary>
        public static bool IsToolMode { get; set; } = false;

        /// <summary>
        /// 允許 JSON-RPC 傳遞資料的型別命名空間清單。
        /// 僅允許這些命名空間中的型別進行反序列化，以確保安全性。
        /// 注意：Bee.Base 與 Bee.Define 為系統內建的預設命名空間，無需額外指定。
        /// </summary>
        public static List<string> AllowedTypeNamespaces { get; set; }

        /// <summary>
        /// 驗證是否為允許的型別命名空間。
        /// </summary>
        /// <param name="typeName">型別名稱。</param>
        public static bool IsTypeNameAllowed(string typeName)
        {
            foreach (var ns in AllowedTypeNamespaces)
            {
                if (typeName.StartsWith(ns + "."))
                    return true;
            }

            return typeName == "System.Byte[]";
        }
    }
}
