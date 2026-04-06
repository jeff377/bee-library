using System.Collections.Generic;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Result collection for a batch package update check.
    /// </summary>
    [MessagePackObject]
    public class CheckPackageUpdateResult : BusinessResult
    {
        /// <summary>
        /// Gets or sets the list of update information items, each corresponding to a query in <see cref="CheckPackageUpdateArgs"/> in order.
        /// </summary>
        [Key(100)]
        public List<PackageUpdateInfo> Updates { get; set; } = new List<PackageUpdateInfo>();
    }
}
