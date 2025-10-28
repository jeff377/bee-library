using System;
using System.Collections.Generic;
using System.Linq;

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
            AllowedTypeNamespaces = new List<string> { "Bee.Base", "Bee.Define", "Bee.Contracts" };
        }

        /// <summary>
        /// 系統主版號。
        /// </summary>
        public static string Version { get; set; } = string.Empty;

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
        /// 是否發佈為單一執行檔（例如 SettingsEditor.exe）。
        /// 若應用程式發佈為單一執行檔時，無法動態載入物件，需由程式碼建立。
        /// </summary>
        public static bool IsSingleFile { get; set; } = false;

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

        /// <summary>
        /// 初始化。
        /// </summary>
        /// <param name="configuration">提供 SysInfo 的相關設定值。</param>
        public static void Initialize(ISysInfoConfiguration configuration)
        {
            Version = configuration.Version;
            IsDebugMode = configuration.IsDebugMode;
            AllowedTypeNamespaces = BuildAllowedTypeNamespaces(configuration.AllowedTypeNamespaces);
        }

        /// <summary>
        /// Parse the list of allowed type namespaces (including system default and user-defined).
        /// </summary>
        /// <param name="customNamespaces">User-defined namespace string, separated by '|'.</param>
        /// <returns>List of namespaces including system default and user-defined.</returns>
        public static List<string> BuildAllowedTypeNamespaces(string customNamespaces)
        {
            // Initialize HashSet to ensure no duplicates
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Bee.Base",
                "Bee.Define",
                "Bee.Contracts"
            };

            // User-defined namespace list (separated by '|')
            // User value may be null, empty, or contain extra separators
            if (!string.IsNullOrWhiteSpace(customNamespaces))
            {
                var parts = customNamespaces.Split('|');
                foreach (var ns in parts)
                {
                    var trimmed = ns.Trim().TrimEnd('.');
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        allowed.Add(trimmed);
                    }
                }
            }

            return allowed.ToList();
        }
    }
}
