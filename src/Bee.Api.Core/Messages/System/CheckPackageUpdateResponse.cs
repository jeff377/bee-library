using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the check package update operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CheckPackageUpdateResponse : ApiResponse, ICheckPackageUpdateResponse
    {
        /// <summary>
        /// Gets or sets the list of available package updates.
        /// </summary>
        public List<PackageUpdateInfo> Updates { get; set; } = [];
    }
}
