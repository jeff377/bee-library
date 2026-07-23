using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the check package update operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CheckPackageUpdateRequest : ApiRequest, ICheckPackageUpdateRequest
    {
        /// <summary>
        /// Gets or sets the list of query items to check.
        /// </summary>
        public List<PackageUpdateQuery> Queries { get; set; } = [];
    }
}
