using MessagePack;
using System.Collections.Generic;

namespace Bee.Define
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
}
