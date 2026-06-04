namespace Bee.Definition.Identity
{
    /// <summary>
    /// A flattened user-role assignment row loaded from <c>st_user_role</c>: the user's business
    /// id and the role's business id assigned to that user (within one company DB). Both are
    /// <c>sys_id</c> business keys — the permission cache works in sys_id, never row ids.
    /// </summary>
    /// <param name="UserId">The user business id (<c>st_user_role.user_id</c> = common <c>st_user.sys_id</c>).</param>
    /// <param name="RoleId">The role business id (<c>st_user_role.role_id</c> = <c>st_role.sys_id</c>).</param>
    public sealed record UserRoleRow(string UserId, string RoleId);
}
