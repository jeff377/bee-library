using System.Collections.Generic;

namespace Bee.Definition.Api
{
    /// <summary>
    /// Contract interface for check package update response data.
    /// </summary>
    public interface ICheckPackageUpdateResponse
    {
        /// <summary>
        /// Gets the list of available package updates.
        /// </summary>
        List<PackageUpdateInfo> Updates { get; }
    }
}
