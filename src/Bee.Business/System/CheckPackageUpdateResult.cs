using System.Collections.Generic;
using Bee.Definition.Api;

namespace Bee.Business.System
{
    /// <summary>
    /// Result collection for a batch package update check.
    /// </summary>
    public class CheckPackageUpdateResult : BusinessResult, ICheckPackageUpdateResponse
    {
        /// <summary>
        /// Gets or sets the list of update information items, each corresponding to a query in <see cref="CheckPackageUpdateArgs"/> in order.
        /// </summary>
        public List<PackageUpdateInfo> Updates { get; set; } = new List<PackageUpdateInfo>();
    }
}
