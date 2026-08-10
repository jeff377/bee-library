using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the check package update operation.
    /// </summary>
    public class CheckPackageUpdateRequest : ApiRequest, ICheckPackageUpdateRequest
    {
        /// <summary>
        /// Gets or sets the list of query items to check.
        /// </summary>
        public List<PackageUpdateQuery> Queries { get; set; } = [];
    }
}
