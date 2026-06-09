namespace Bee.Definition.Identity
{
    /// <summary>
    /// The record-scope identity of the current user within an entered company, resolved once at
    /// <c>EnterCompany</c> and snapshotted onto <c>SessionInfo</c> so scope filtering runs from memory.
    /// </summary>
    /// <param name="UserRowId">The user row id (<c>st_user.sys_rowid</c>); used by the <c>Own</c> scope.</param>
    /// <param name="EmployeeRowId">The linked employee row id (<c>st_employee.sys_rowid</c>); <see cref="System.Guid.Empty"/> when the user has no employee in this company. Used by the <c>Own</c> scope.</param>
    /// <param name="DeptRowId">The employee's department row id (<c>st_employee.dept_rowid</c>); <see cref="System.Guid.Empty"/> when the user has no employee or no department. Used by the <c>Dept</c> / <c>DeptAndSub</c> scopes.</param>
    public sealed record EmployeeContext(Guid UserRowId, Guid EmployeeRowId, Guid DeptRowId)
    {
        /// <summary>An empty context (no user/employee/department resolved).</summary>
        public static EmployeeContext Empty { get; } = new(Guid.Empty, Guid.Empty, Guid.Empty);
    }
}
