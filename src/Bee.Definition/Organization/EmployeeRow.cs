namespace Bee.Definition.Organization
{
    /// <summary>
    /// A flat employee row as read from <c>st_employee</c> (a company-database table), carrying the
    /// links needed to resolve the current user's department: the account link (<c>user_rowid</c>)
    /// and the department (<c>dept_rowid</c>).
    /// </summary>
    /// <param name="RowId">The employee row id (<c>st_employee.sys_rowid</c>).</param>
    /// <param name="EmployeeId">The employee business id (<c>sys_id</c>).</param>
    /// <param name="EmployeeName">The employee name (<c>sys_name</c>).</param>
    /// <param name="DeptRowId">The department row id (<c>dept_rowid</c>); <see cref="System.Guid.Empty"/> when unassigned.</param>
    /// <param name="UserRowId">The linked user row id (<c>user_rowid</c>, logically points at common <c>st_user.sys_rowid</c>); <see cref="System.Guid.Empty"/> when unlinked.</param>
    public sealed record EmployeeRow(
        Guid RowId,
        string EmployeeId,
        string EmployeeName,
        Guid DeptRowId,
        Guid UserRowId);
}
