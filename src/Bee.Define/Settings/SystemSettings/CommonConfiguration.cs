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
    [TreeNode("通用")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CommonConfiguration
    {
        /// <summary>
        /// 系統主版琥。
        /// </summary>
        [Description("系統版號。")]
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
        [DefaultValue("")]
        public ApiPayloadOptions ApiPayloadOptions { get; set; } = new ApiPayloadOptions();

        /// <summary>
        /// 初始化。
        /// </summary>
        public void Initialize()
        {
            SysInfo.Version = Version;
            SysInfo.IsDebugMode = IsDebugMode;

            // 初始化 HashSet 以確保不重複
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Bee.Base",
                "Bee.Define"
            };

            // 使用者自訂命名空間清單（以 '|' 分隔）
            // 使用者設定值可能為 null、空白或帶多餘的分隔符
            if (!string.IsNullOrWhiteSpace(AllowedTypeNamespaces))
            {
                var parts = AllowedTypeNamespaces.Split('|');
                foreach (var ns in parts)
                {
                    var trimmed = ns.Trim().TrimEnd('.');
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        allowed.Add(trimmed);
                    }
                }
            }

            // 寫入 SysInfo.AllowedTypeNamespaces，全系統可用
            SysInfo.AllowedTypeNamespaces = allowed.ToList();
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
