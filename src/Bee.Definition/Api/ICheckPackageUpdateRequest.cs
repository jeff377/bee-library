using System.Collections.Generic;

namespace Bee.Definition.Api
{
    /// <summary>
    /// Contract interface for check package update request parameters.
    /// </summary>
    public interface ICheckPackageUpdateRequest
    {
        /// <summary>
        /// Gets the list of package update queries.
        /// </summary>
        List<PackageUpdateQuery> Queries { get; }
    }
}
