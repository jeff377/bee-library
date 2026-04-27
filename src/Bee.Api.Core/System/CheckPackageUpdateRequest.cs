using Bee.Api.Contracts;
using MessagePack;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API request for the check package update operation.
    /// </summary>
    [MessagePackObject]
    public class CheckPackageUpdateRequest : ApiRequest, ICheckPackageUpdateRequest
    {
        /// <summary>
        /// Gets or sets the list of query items to check.
        /// </summary>
        [Key(100)]
        public List<PackageUpdateQuery> Queries { get; set; } = [];
    }
}
