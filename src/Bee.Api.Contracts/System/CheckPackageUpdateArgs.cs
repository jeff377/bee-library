using System.Collections.Generic;
using MessagePack;

namespace Bee.Api.Contracts.System
{
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
}
