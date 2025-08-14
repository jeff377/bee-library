using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 指定要檢查更新的套件查詢項目（App/Component + 目前版本 + 平台/通道）。
    /// </summary>
    [MessagePackObject]
    public sealed class PackageUpdateQuery
    {
        /// <summary>
        /// 應用程式/工具代號（例：Client、SettingsEditor、DefinitionTool、DbUpgrade、FlowService）。
        /// </summary>
        [Key(0)]
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// 元件代號（例：Main、Reference、Plugin-XYZ）。未指定時預設為 Main。
        /// </summary>
        [Key(1)]
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// 目前安裝版本字串（例：1.2.3）。首次未安裝可傳空字串。
        /// </summary>
        [Key(2)]
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// 執行平台（例：Win-x64、Win-arm64、macOS）。
        /// </summary>
        [Key(3)]
        public string Platform { get; set; } = "Win-x64";

        /// <summary>
        /// 發佈通道（例：Stable、Beta）。
        /// </summary>
        [Key(4)]
        public string Channel { get; set; } = "Stable";
    }
}
