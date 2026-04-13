using System.Collections.Generic;
using Bee.Definition.Api;

namespace Bee.Business.System
{
    /// <summary>
    /// Arguments for batch-checking whether multiple apps or components have available updates.
    /// </summary>
    public class CheckPackageUpdateArgs : BusinessArgs, ICheckPackageUpdateRequest
    {
        /// <summary>
        /// Gets or sets the list of query items to check.
        /// </summary>
        public List<PackageUpdateQuery> Queries { get; set; } = new List<PackageUpdateQuery>();
    }
}
