using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 通用參數及環境設置。
    /// </summary>
    [Serializable]
    [XmlType("CommonConfiguration")]
    [Description("通用參數及環境設置。")]
    [TreeNode("Common")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CommonConfiguration : IObjectSerializeBase
    {
        /// <summary>
        /// 系統主版號。
        /// </summary>
        [Description("系統主版號。")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 是否為偵錯模式。
        /// </summary>
        [Description("是否為偵錯模式")]
        [DefaultValue(false)]
        public bool IsDebugMode { get; set; } = false;

        /// <summary>
        /// 允許 JSON-RPC 傳遞資料的型別命名空間清單（以 '|' 分隔）。
        /// 僅允許這些命名空間中的型別進行反序列化，以確保安全性。
        /// 設定格式範例：Custom.Module|ThirdParty.Dto
        /// 注意：Bee.Base 與 Bee.Define 為系統內建的預設命名空間，無需額外指定。
        /// </summary>
        [Category("API")]
        [Description("允許 JSON-RPC 傳遞資料的型別命名空間清單，以 '|' 分隔。")]
        [DefaultValue("")]
        public string AllowedTypeNamespaces { get; set; } = string.Empty;

        /// <summary>
        /// 提供 API Payload 處理相關選項，例如序列化、壓縮與加密。
        /// </summary>
        [Category("API")]
        [Description("提供 API Payload 處理相關選項，例如序列化、壓縮與加密。")]
        public ApiPayloadOptions ApiPayloadOptions { get; set; } = new ApiPayloadOptions();

        /// <summary>
        /// 記錄選項，用於設定日誌記錄的相關參數。
        /// </summary>
        [Category("Logging")]
        [Description("提供日誌記錄的相關選項，例如記錄層級、輸出格式等。")]
        public LogOptions LogOptions { get; set; } = new LogOptions();

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            SysInfo.Version = Version;
            SysInfo.IsDebugMode = IsDebugMode;
            // 解析允許的型別命名空間清單
            SysInfo.AllowedTypeNamespaces = BuildAllowedTypeNamespaces(AllowedTypeNamespaces);
            // 記錄選項
            SysInfo.LogOptions = LogOptions;
        }

        /// <summary>
        /// 解析允許的型別命名空間清單（包含系統預設與使用者設定）。
        /// </summary>
        /// <param name="customNamespaces">使用者自訂的命名空間字串，以 '|' 分隔。</param>
        /// <returns>包含系統預設與使用者自訂命名空間的清單。</returns>
        public static List<string> BuildAllowedTypeNamespaces(string customNamespaces)
        {
            // 初始化 HashSet 以確保不重複
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Bee.Base",
                "Bee.Define"
            };

            // 使用者自訂命名空間清單（以 '|' 分隔）
            // 使用者設定值可能為 null、空白或帶多餘的分隔符
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

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return "CommonConfiguration";
        }
    }
}
