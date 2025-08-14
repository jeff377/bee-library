using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Define
{
    /// <summary>
    /// 單一查詢項目的更新資訊。
    /// </summary>
    [MessagePackObject]
    public sealed class PackageUpdateInfo
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
