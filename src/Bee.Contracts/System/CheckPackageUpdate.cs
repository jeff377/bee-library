using MessagePack;
using System.Collections.Generic;

namespace Bee.Contracts
{
    /// <summary>
    /// 套件發佈方式。序列化時以整數值表示（0/1），請勿變更既有成員的數值。
    /// </summary>
    public enum PackageDelivery : int
    {
        /// <summary>
        /// 回傳短時效 URL 供直接下載（建議用於大檔）。
        /// </summary>
        Url = 0,
        /// <summary>
        /// 由 API 直接傳回位元組內容（小檔或內部環境）。
        /// </summary>
        Api = 1
    }

    /// <summary>
    /// 批次檢查多個 App/Component 是否有可用更新的引數。
    /// </summary>
    [MessagePackObject]
    public class CheckPackageUpdateArgs : BusinessArgs
    {
        /// <summary>
        /// 要檢查的多筆查詢項目清單。
        /// </summary>
        [Key(100)]
        public List<PackageUpdateQuery> Queries { get; set; } = new List<PackageUpdateQuery>();
    }

    /// <summary>
    /// 批次檢查更新的回傳結果集合。
    /// </summary>
    [MessagePackObject]
    public class CheckPackageUpdateResult : BusinessResult
    {
        /// <summary>
        /// 逐項回傳的更新資訊清單（與 <see cref="CheckPackageUpdateArgs"/> 的查詢順序對應）。
        /// </summary>
        [Key(100)]
        public List<PackageUpdateInfo> Updates { get; set; } = new List<PackageUpdateInfo>();
    }

    /// <summary>
    /// 指定要檢查更新的套件查詢項目（App/Component + 目前版本 + 平台/通道）。
    /// </summary>
    [MessagePackObject]
    public class PackageUpdateQuery
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

    /// <summary>
    /// 單一查詢項目的更新資訊。
    /// </summary>
    [MessagePackObject]
    public class PackageUpdateInfo
    {
        /// <summary>
        /// 對應的應用程式/工具代號。
        /// </summary>
        [Key(0)]
        public string AppId { get; set; } = string.Empty;

        /// <summary>
        /// 對應的元件代號。
        /// </summary>
        [Key(1)]
        public string ComponentId { get; set; } = "Main";

        /// <summary>
        /// 是否有可用更新。
        /// </summary>
        [Key(2)]
        public bool UpdateAvailable { get; set; }

        /// <summary>
        /// 最新版本字串（例：1.2.4）。
        /// </summary>
        [Key(3)]
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>
        /// 是否為強制更新（true 代表必須升級後才能繼續使用）。
        /// </summary>
        [Key(4)]
        public bool Mandatory { get; set; }

        /// <summary>
        /// 套件大小（位元組）。
        /// </summary>
        [Key(5)]
        public long PackageSize { get; set; }

        /// <summary>
        /// 套件檔案的 SHA256（十六進位字串），供下載後完整性驗證。
        /// </summary>
        [Key(6)]
        public string Sha256 { get; set; } = string.Empty;

        /// <summary>
        /// 發佈方式（Url / Api）。
        /// </summary>
        [Key(7)]
        public PackageDelivery Delivery { get; set; } = PackageDelivery.Url;

        /// <summary>
        /// 若為 Url 發佈，提供短時效下載連結。
        /// </summary>
        [Key(8)]
        public string PackageUrl { get; set; } = string.Empty;

        /// <summary>
        /// 版本摘要/釋出說明（可為純文字或 Markdown）。
        /// </summary>
        [Key(9)]
        public string ReleaseNotes { get; set; } = string.Empty;

        // 之後新增欄位請從 Key(10) 往後排
    }
}
