using Bee.Definition.Organization;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the get department tree response.
    /// </summary>
    public interface IGetDepartmentTreeResponse
    {
        /// <summary>
        /// Gets the current company's department tree (<c>null</c> when no company is entered).
        /// </summary>
        DepartmentTree? Tree { get; }
    }
}
