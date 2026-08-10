using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the check package update operation.
    /// </summary>
    public class CheckPackageUpdateResponse : ApiResponse, ICheckPackageUpdateResponse
    {
        /// <summary>
        /// Gets or sets the list of available package updates.
        /// </summary>
        public List<PackageUpdateInfo> Updates { get; set; } = [];
    }
}
