namespace Bee.Definition.Identity
{
    /// <summary>
    /// A flattened user-role assignment row loaded from the permission tables: the user's
    /// row id and the role's business id assigned to that user (within one company DB).
    /// </summary>
    /// <param name="UserRowId">The user row id (<c>st_user_role.user_rowid</c>, a GUID string; logically references common <c>st_user.sys_rowid</c>).</param>
    /// <param name="RoleId">The role business id (<c>st_role.sys_id</c>).</param>
    public sealed record UserRoleRow(string UserRowId, string RoleId);
}
