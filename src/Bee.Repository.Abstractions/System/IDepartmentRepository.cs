using Bee.Definition.Organization;

namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Data access for a company's department table (<c>ft_department</c>). Lives in a company
    /// database, so the method takes the company database id explicitly (resolved by the caller
    /// via the company-DB router).
    /// </summary>
    public interface IDepartmentRepository
    {
        /// <summary>
        /// Reads every department row from the company database's <c>ft_department</c> table as flat
        /// <see cref="DepartmentRow"/> carriers (with the parent pointer); the in-memory
        /// <see cref="DepartmentTree"/> assembles them into the nested hierarchy.
        /// </summary>
        /// <param name="databaseId">The company database id.</param>
        IReadOnlyList<DepartmentRow> GetDepartments(string databaseId);
    }
}
