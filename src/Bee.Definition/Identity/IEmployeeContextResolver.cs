namespace Bee.Definition.Identity
{
    /// <summary>
    /// Resolves the current user's record-scope identity (<see cref="EmployeeContext"/>) by linking
    /// the common <c>st_user</c> account to its <c>st_employee</c> record in a company database.
    /// Invoked once per <c>EnterCompany</c>; the result is snapshotted onto the session.
    /// </summary>
    public interface IEmployeeContextResolver
    {
        /// <summary>
        /// Resolves the user's <see cref="EmployeeContext"/> within the given company database.
        /// Returns <see cref="EmployeeContext.Empty"/> when the user account cannot be found, and a
        /// context with an empty employee/department when the account has no linked employee.
        /// </summary>
        /// <param name="userId">The user business id (<c>st_user.sys_id</c>).</param>
        /// <param name="databaseId">The company database id (where <c>st_employee</c> lives).</param>
        EmployeeContext Resolve(string userId, string databaseId);
    }
}
