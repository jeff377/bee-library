using Bee.Definition.Settings;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// A flattened role-grant row loaded from <c>st_role_grant</c>: one (role, model, action) the
    /// role is granted, plus the record-scope strategy for that action. Presence of the row is the
    /// layer-1 grant; <see cref="Scope"/> drives layer-2 record-scope filtering (<see cref="ScopeStrategy.Inherit"/>
    /// defers to the permission model's default scope for the action).
    /// </summary>
    /// <param name="RoleId">The role business id (<c>st_role_grant.role_id</c> = <c>st_role.sys_id</c>).</param>
    /// <param name="ModelId">The permission model id (<c>st_role_grant.model_id</c>).</param>
    /// <param name="Action">The single granted action (<c>st_role_grant.action</c>).</param>
    /// <param name="Scope">The record-scope strategy for this (role, model, action).</param>
    public sealed record RoleGrantRow(string RoleId, string ModelId, PermissionAction Action, ScopeStrategy Scope);
}
