using System.Collections.Generic;
using MessagePack;

namespace Bee.Api.Contracts.System
{
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
