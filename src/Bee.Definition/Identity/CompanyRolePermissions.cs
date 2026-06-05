using Bee.Base;
using Bee.Definition.Settings;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// A per-company permission snapshot loaded from the company database's permission tables:
    /// <see cref="Grants"/> (role→model→action, for the <c>Can</c> check) and <see cref="UserRoles"/>
    /// (user→role, for <c>EnterCompany</c> to fill <c>SessionInfo.Roles</c>). Cached per company so
    /// permission checks run entirely from memory — DB is touched only when (re)loading the snapshot.
    /// </summary>
    public sealed class CompanyRolePermissions : IKeyObject
    {
        /// <summary>
        /// Initializes a new <see cref="CompanyRolePermissions"/>.
        /// </summary>
        /// <param name="companyId">The company id (cache key).</param>
        /// <param name="grants">The role grants (role→model→action).</param>
        /// <param name="userRoles">The user-role assignments (user→role).</param>
        public CompanyRolePermissions(string companyId, IReadOnlyList<RoleGrantRow> grants, IReadOnlyList<UserRoleRow> userRoles)
        {
            CompanyId = companyId ?? throw new ArgumentNullException(nameof(companyId));
            Grants = grants ?? throw new ArgumentNullException(nameof(grants));
            UserRoles = userRoles ?? throw new ArgumentNullException(nameof(userRoles));
        }

        /// <summary>
        /// Gets the item key value (the company id).
        /// </summary>
        public string GetKey() => CompanyId;

        /// <summary>Gets the company id (cache key).</summary>
        public string CompanyId { get; }

        /// <summary>Gets the role grants (role business id → model → allowed action mask).</summary>
        public IReadOnlyList<RoleGrantRow> Grants { get; }

        /// <summary>Gets the user-role assignments (user business id → role business id).</summary>
        public IReadOnlyList<UserRoleRow> UserRoles { get; }

        /// <summary>
        /// Returns the OR-merged allowed action mask for the given roles on the model — the layer-1
        /// multi-role union (capability accrues across roles). Returns <see cref="PermissionActions.None"/>
        /// when none of the roles grants anything on the model.
        /// </summary>
        /// <param name="roleIds">The role business ids the user holds (e.g. <c>SessionInfo.Roles</c>).</param>
        /// <param name="modelId">The permission model id to check.</param>
        public PermissionActions GetAllowed(IEnumerable<string> roleIds, string modelId)
        {
            ArgumentNullException.ThrowIfNull(roleIds);
            var roleSet = roleIds as ISet<string> ?? new HashSet<string>(roleIds);

            var allowed = PermissionActions.None;
            foreach (var grant in Grants)
            {
                if (grant.ModelId == modelId && roleSet.Contains(grant.RoleId))
                {
                    allowed |= grant.AllowedActions;
                }
            }
            return allowed;
        }

        /// <summary>
        /// Gets the role business ids assigned to the given user — used by <c>EnterCompany</c> to
        /// populate <c>SessionInfo.Roles</c> from <c>SessionInfo.UserId</c> without touching the database.
        /// </summary>
        /// <param name="userId">The user business id (<c>SessionInfo.UserId</c> = <c>st_user.sys_id</c>).</param>
        public IReadOnlyList<string> GetUserRoleIds(string userId)
        {
            var list = new List<string>();
            foreach (var assignment in UserRoles)
            {
                if (assignment.UserId == userId)
                {
                    list.Add(assignment.RoleId);
                }
            }
            return list;
        }
    }
}
