using System.Collections.Generic;
using Bee.Definition.Api;
using MessagePack;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API response for the check package update operation.
    /// </summary>
    [MessagePackObject]
    public class CheckPackageUpdateResponse : ApiResponse, ICheckPackageUpdateResponse
    {
        /// <summary>
        /// Gets or sets the list of available package updates.
        /// </summary>
        [Key(100)]
        public List<PackageUpdateInfo> Updates { get; set; } = new List<PackageUpdateInfo>();
    }
}
