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
        /// Reads every department node from the company database's <c>ft_department</c> table.
        /// </summary>
        /// <param name="databaseId">The company database id.</param>
        IReadOnlyList<DepartmentNode> GetDepartments(string databaseId);
    }
}
