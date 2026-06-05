using Bee.Definition.Organization;

namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Data access for a company's employee table (<c>ft_employee</c>). Lives in a company database,
    /// so methods take the company database id explicitly (resolved by the caller via the
    /// company-DB router).
    /// </summary>
    public interface IEmployeeRepository
    {
        /// <summary>
        /// Reads the employee linked to the given user (<c>user_rowid</c>) from the company database,
        /// or <c>null</c> when the user has no employee record there.
        /// </summary>
        /// <param name="databaseId">The company database id.</param>
        /// <param name="userRowId">The user row id (common <c>st_user.sys_rowid</c>).</param>
        EmployeeRow? GetByUserRowId(string databaseId, Guid userRowId);
    }
}
