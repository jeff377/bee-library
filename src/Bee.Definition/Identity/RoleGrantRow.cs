using Bee.Definition.Settings;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// A flattened role-grant row loaded from the permission tables: the role's business id,
    /// the permission model, and the allowed action mask for that (role, model) pair.
    /// </summary>
    /// <param name="RoleId">The role business id (<c>st_role.sys_id</c>).</param>
    /// <param name="ModelId">The permission model id (<c>st_role_grant.model_id</c>).</param>
    /// <param name="AllowedActions">The allowed action mask for this (role, model).</param>
    public sealed record RoleGrantRow(string RoleId, string ModelId, PermissionActions AllowedActions);
}
