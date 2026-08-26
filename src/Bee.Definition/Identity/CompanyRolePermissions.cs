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
    /// <remarks>
    /// WARNING: this is a cache-shared instance. It must not be mutated after construction — every
    /// session in the company receives the same reference, and this one carries authorization state,
    /// so a mutation grants or denies access in other sessions. The type exposes no setters, which
    /// makes the rule structural rather than a convention; keep it that way. See
    /// <c>docs/development-constraints.md</c> § <i>Cached Data Immutability After Init</i>.
    /// </remarks>
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

            // WARNING: The indexes are built here, once, and never mutated afterwards. This type is an
            // immutable snapshot held in a process-wide cache, so paying O(|Grants|) at construction
            // buys every later lookup — and the lookups are on the authorization path, where the
            // previous linear scans ran per check.
            //
            // |Grants| is roles × models, which is thousands to tens of thousands in an ERP: measured
            // at 200 roles × 100 models, a single Save spent 93 µs scanning, approaching the cost of a
            // database round-trip. The scans also allocated a HashSet per call, because
            // `SessionInfo.Roles` is a List and the `as ISet` test therefore never succeeded.
            _allowedByRole = BuildAllowedByRole(Grants);
            _scopesByRoleModelAction = BuildScopes(Grants);
            _rolesByUser = BuildRolesByUser(UserRoles);
        }

        /// <summary>
        /// Gets the item key value (the company id).
        /// </summary>
        public string GetKey() => CompanyId;

        // Identifier keys → Ordinal (culture-invariant and fastest); see rules/code-style.md.
        private readonly Dictionary<string, Dictionary<string, PermissionAction>> _allowedByRole;
        private readonly Dictionary<(string RoleId, string ModelId, PermissionAction Action), List<ScopeStrategy>> _scopesByRoleModelAction;
        private readonly Dictionary<string, List<string>> _rolesByUser;

        private static Dictionary<string, Dictionary<string, PermissionAction>> BuildAllowedByRole(
            IReadOnlyList<RoleGrantRow> grants)
        {
            var byRole = new Dictionary<string, Dictionary<string, PermissionAction>>(StringComparer.Ordinal);
            foreach (var grant in grants)
            {
                if (!byRole.TryGetValue(grant.RoleId, out var byModel))
                {
                    byModel = new Dictionary<string, PermissionAction>(StringComparer.Ordinal);
                    byRole[grant.RoleId] = byModel;
                }
                byModel.TryGetValue(grant.ModelId, out var current);
                byModel[grant.ModelId] = current | grant.Action;
            }
            return byRole;
        }

        private static Dictionary<(string, string, PermissionAction), List<ScopeStrategy>> BuildScopes(
            IReadOnlyList<RoleGrantRow> grants)
        {
            var byKey = new Dictionary<(string, string, PermissionAction), List<ScopeStrategy>>();
            foreach (var grant in grants)
            {
                var key = (grant.RoleId, grant.ModelId, grant.Action);
                if (!byKey.TryGetValue(key, out var scopes))
                {
                    scopes = [];
                    byKey[key] = scopes;
                }
                scopes.Add(grant.Scope);
            }
            return byKey;
        }

        private static Dictionary<string, List<string>> BuildRolesByUser(IReadOnlyList<UserRoleRow> userRoles)
        {
            var byUser = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var assignment in userRoles)
            {
                if (!byUser.TryGetValue(assignment.UserId, out var roles))
                {
                    roles = [];
                    byUser[assignment.UserId] = roles;
                }
                roles.Add(assignment.RoleId);
            }
            return byUser;
        }

        /// <summary>Gets the company id (cache key).</summary>
        public string CompanyId { get; }

        /// <summary>Gets the role grants (role business id → model → allowed action mask).</summary>
        public IReadOnlyList<RoleGrantRow> Grants { get; }

        /// <summary>Gets the user-role assignments (user business id → role business id).</summary>
        public IReadOnlyList<UserRoleRow> UserRoles { get; }

        /// <summary>
        /// Returns the OR-merged allowed action mask for the given roles on the model — the layer-1
        /// multi-role union (capability accrues across roles). Returns <see cref="PermissionAction.None"/>
        /// when none of the roles grants anything on the model.
        /// </summary>
        /// <param name="roleIds">The role business ids the user holds (e.g. <c>SessionInfo.Roles</c>).</param>
        /// <param name="modelId">The permission model id to check.</param>
        public PermissionAction GetAllowed(IEnumerable<string> roleIds, string modelId)
        {
            ArgumentNullException.ThrowIfNull(roleIds);

            // Iterates the roles the user holds (a handful) rather than every grant in the company.
            var allowed = PermissionAction.None;
            foreach (var roleId in roleIds)
            {
                if (_allowedByRole.TryGetValue(roleId, out var byModel) &&
                    byModel.TryGetValue(modelId, out var action))
                {
                    allowed |= action;
                }
            }
            return allowed;
        }

        /// <summary>
        /// Returns the OR-merged allowed action mask per model for the given roles — the full
        /// capability snapshot handed to the client on <c>EnterCompany</c>. Only models the roles
        /// hold at least one grant on appear as keys; a model absent from the result means no
        /// permission (the client resolver treats an absent model as denied). Freshly built each
        /// call, so the caller owns the returned dictionary.
        /// </summary>
        /// <param name="roleIds">The role business ids the user holds (e.g. <c>SessionInfo.Roles</c>).</param>
        public Dictionary<string, PermissionAction> GetAllowedByModel(IEnumerable<string> roleIds)
        {
            ArgumentNullException.ThrowIfNull(roleIds);

            // Model ids are identifiers → Ordinal comparison (culture-invariant, fastest).
            var result = new Dictionary<string, PermissionAction>(StringComparer.Ordinal);
            foreach (var roleId in roleIds)
            {
                if (!_allowedByRole.TryGetValue(roleId, out var byModel)) { continue; }
                foreach (var pair in byModel)
                {
                    result.TryGetValue(pair.Key, out var current);
                    result[pair.Key] = current | pair.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the record-scope strategies the given roles grant for the (model, action) — one per
        /// role that grants the action (layer-2 input). The strategies are raw grant values (may be
        /// <see cref="ScopeStrategy.Inherit"/>); the resolver resolves <c>Inherit</c> against the
        /// permission model's default and merges across roles. Empty when no held role grants the action.
        /// </summary>
        /// <param name="roleIds">The role business ids the user holds (e.g. <c>SessionInfo.Roles</c>).</param>
        /// <param name="modelId">The permission model id to check.</param>
        /// <param name="action">The single action to check.</param>
        public IReadOnlyList<ScopeStrategy> GetEffectiveScopes(IEnumerable<string> roleIds, string modelId, PermissionAction action)
        {
            ArgumentNullException.ThrowIfNull(roleIds);

            // NOTE: The result order now follows `roleIds` rather than `Grants`. The only caller
            // (`ScopeResolver`) folds these into a HashSet and short-circuits on `All`, so order
            // carries no meaning — this is a deliberate check, not an assumption.
            var scopes = new List<ScopeStrategy>();
            foreach (var roleId in roleIds)
            {
                if (_scopesByRoleModelAction.TryGetValue((roleId, modelId, action), out var granted))
                {
                    scopes.AddRange(granted);
                }
            }
            return scopes;
        }

        /// <summary>
        /// Gets the role business ids assigned to the given user — used by <c>EnterCompany</c> to
        /// populate <c>SessionInfo.Roles</c> from <c>SessionInfo.UserId</c> without touching the database.
        /// </summary>
        /// <param name="userId">The user business id (<c>SessionInfo.UserId</c> = <c>st_user.sys_id</c>).</param>
        public IReadOnlyList<string> GetUserRoleIds(string userId)
        {
            return _rolesByUser.TryGetValue(userId, out var roles) ? roles : [];
        }
    }
}
