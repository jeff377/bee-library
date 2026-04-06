using System.Collections.Generic;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Arguments for batch-checking whether multiple apps or components have available updates.
    /// </summary>
    [MessagePackObject]
    public class CheckPackageUpdateArgs : BusinessArgs
    {
        /// <summary>
        /// Gets or sets the list of query items to check.
        /// </summary>
        [Key(100)]
        public List<PackageUpdateQuery> Queries { get; set; } = new List<PackageUpdateQuery>();
    }
}
