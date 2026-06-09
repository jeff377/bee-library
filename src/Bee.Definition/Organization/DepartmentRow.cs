namespace Bee.Definition.Organization
{
    /// <summary>
    /// A flat department row as read from <c>st_department</c>, carrying the parent pointer
    /// (<c>parent_rowid</c>) used to assemble the hierarchy. This is the load-time carrier only;
    /// the serialised tree node is the nested <see cref="DepartmentNode"/>, which holds its
    /// <see cref="DepartmentNode.Children"/> rather than a parent id.
    /// </summary>
    /// <param name="RowId">The department row id (<c>st_department.sys_rowid</c>).</param>
    /// <param name="DeptId">The department business id (<c>sys_id</c>).</param>
    /// <param name="DeptName">The department name (<c>sys_name</c>).</param>
    /// <param name="ParentRowId">The parent department row id; <see cref="System.Guid.Empty"/> for a root.</param>
    /// <param name="ManagerRowId">The manager (employee) row id.</param>
    public sealed record DepartmentRow(
        Guid RowId,
        string DeptId,
        string DeptName,
        Guid ParentRowId,
        Guid ManagerRowId);
}
