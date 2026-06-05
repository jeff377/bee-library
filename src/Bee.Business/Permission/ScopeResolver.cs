using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Business.Permission
{
    /// <summary>
    /// Default <see cref="IScopeResolver"/>. Reads the session's record-scope identity snapshot, the
    /// roles' granted scope strategies (via the per-company permission snapshot), the permission
    /// model's default scope (for <c>Inherit</c> grants), and the department tree, then produces a
    /// read filter or a per-row verdict. Pure in-memory once the snapshots are cached.
    /// </summary>
    public sealed class ScopeResolver : IScopeResolver
    {
        private readonly ISessionInfoService _sessionInfoService;
        private readonly IRolePermissionService _rolePermissionService;
        private readonly IDepartmentTreeService _departmentTreeService;
        private readonly IDefineAccess _defineAccess;

        /// <summary>
        /// Initializes a new <see cref="ScopeResolver"/>.
        /// </summary>
        public ScopeResolver(
            ISessionInfoService sessionInfoService,
            IRolePermissionService rolePermissionService,
            IDepartmentTreeService departmentTreeService,
            IDefineAccess defineAccess)
        {
            _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
            _rolePermissionService = rolePermissionService ?? throw new ArgumentNullException(nameof(rolePermissionService));
            _departmentTreeService = departmentTreeService ?? throw new ArgumentNullException(nameof(departmentTreeService));
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        }

        /// <inheritdoc/>
        public FilterNode? ResolveFilter(Guid accessToken, string modelId, PermissionAction action, FormSchema formSchema)
        {
            var session = _sessionInfoService.Get(accessToken);
            var scopes = ResolveScopes(session, modelId, action);
            if (scopes == null) { return null; }                                  // unrestricted (All)
            if (scopes.Count == 0 || session == null) { return DenyAll(formSchema); }

            var master = formSchema?.MasterTable;
            var ownerField = master?.GetOwnerField()?.FieldName;
            var deptField = master?.GetDeptField()?.FieldName;
            var ownerIds = OwnerIdentities(session);

            var nodes = new List<FilterNode>();
            foreach (var scope in scopes)
            {
                var node = BuildScopePredicate(scope, session, ownerField, deptField, ownerIds);
                if (node != null) { nodes.Add(node); }
            }
            if (nodes.Count == 0) { return DenyAll(formSchema); }
            return nodes.Count == 1 ? nodes[0] : FilterGroup.Any(nodes.ToArray());
        }

        // ---- scope resolution + multi-role merge ----

        // Returns null for an unrestricted scope (any role grants All); otherwise the distinct set of
        // restrictive strategies to OR-union. An empty set means deny (no usable grant — fail closed).
        private IReadOnlyList<ScopeStrategy>? ResolveScopes(SessionInfo? session, string modelId, PermissionAction action)
        {
            if (session == null || string.IsNullOrEmpty(session.CompanyId)) { return []; }

            var snapshot = _rolePermissionService.Get(session.CompanyId);
            if (snapshot == null) { return []; }

            var raw = snapshot.GetEffectiveScopes(session.Roles, modelId, action);
            if (raw.Count == 0) { return []; }                                    // layer-1 should block first

            PermissionModels? models = null;
            var resolved = new HashSet<ScopeStrategy>();
            foreach (var rawScope in raw)
            {
                var scope = rawScope;
                if (scope == ScopeStrategy.Inherit)
                {
                    models ??= _defineAccess.GetPermissionModels();
                    scope = ResolveInherit(models, modelId, action);
                }
                if (scope == ScopeStrategy.All) { return null; }                  // unrestricted short-circuit
                resolved.Add(scope);
            }
            return [.. resolved];
        }

        // Resolves an Inherit grant scope to a concrete strategy: the model's per-action default, else
        // the model's Read default (egress inherits Read), else All (no scope configured → unrestricted).
        private static ScopeStrategy ResolveInherit(PermissionModels models, string modelId, PermissionAction action)
        {
            var scope = ModelDefault(models, modelId, action);
            if (scope == ScopeStrategy.Inherit) { scope = ModelDefault(models, modelId, PermissionAction.Read); }
            return scope == ScopeStrategy.Inherit ? ScopeStrategy.All : scope;
        }

        private static ScopeStrategy ModelDefault(PermissionModels models, string modelId, PermissionAction action)
        {
            if (models?.Models == null || !models.Models.Contains(modelId)) { return ScopeStrategy.Inherit; }
            var rules = models.Models[modelId].Rules;
            var key = action.ToString();
            if (rules == null || !rules.Contains(key)) { return ScopeStrategy.Inherit; }
            return rules[key].Scope;
        }

        // ---- read-side predicates ----

        private FilterNode? BuildScopePredicate(ScopeStrategy scope, SessionInfo session, string? ownerField, string? deptField, IReadOnlyList<object> ownerIds)
        {
            return scope switch
            {
                ScopeStrategy.Own => OwnPredicate(ownerField, ownerIds),
                ScopeStrategy.Dept => OrParts(DeptEqualPredicate(deptField, session.DeptRowId), OwnPredicate(ownerField, ownerIds)),
                ScopeStrategy.DeptAndSub => OrParts(DeptSubtreePredicate(deptField, session), OwnPredicate(ownerField, ownerIds)),
                _ => null,
            };
        }

        // ownerField IN (UserRowId, EmployeeRowId). An empty id set renders as "1 = 0" (deny).
        private static FilterCondition? OwnPredicate(string? ownerField, IReadOnlyList<object> ownerIds)
        {
            if (string.IsNullOrEmpty(ownerField)) { return null; }
            return new FilterCondition { FieldName = ownerField, Operator = ComparisonOperator.In, Value = ownerIds };
        }

        private static FilterCondition? DeptEqualPredicate(string? deptField, Guid deptRowId)
        {
            if (string.IsNullOrEmpty(deptField) || deptRowId == Guid.Empty) { return null; }
            return FilterCondition.Equal(deptField, deptRowId);
        }

        private FilterCondition? DeptSubtreePredicate(string? deptField, SessionInfo session)
        {
            if (string.IsNullOrEmpty(deptField) || session.DeptRowId == Guid.Empty) { return null; }
            var tree = _departmentTreeService.Get(session.CompanyId!);
            var subtree = tree?.GetSelfAndDescendants(session.DeptRowId) ?? [];
            if (subtree.Count == 0) { return null; }
            var values = new List<object>(subtree.Count);
            foreach (var id in subtree) { values.Add(id); }
            return new FilterCondition { FieldName = deptField, Operator = ComparisonOperator.In, Value = values };
        }

        private static FilterNode? OrParts(FilterNode? a, FilterNode? b)
        {
            var parts = new List<FilterNode>(2);
            if (a != null) { parts.Add(a); }
            if (b != null) { parts.Add(b); }
            if (parts.Count == 0) { return null; }
            return parts.Count == 1 ? parts[0] : FilterGroup.Any(parts.ToArray());
        }

        // Always-false node ("<field> IN ()" → "1 = 0"). Uses a field guaranteed to be in the master
        // table's select context so field remapping does not choke.
        private static FilterCondition DenyAll(FormSchema? formSchema)
        {
            return new FilterCondition { FieldName = AnyMasterFieldName(formSchema), Operator = ComparisonOperator.In, Value = new List<object>() };
        }

        private static string AnyMasterFieldName(FormSchema? formSchema)
        {
            var master = formSchema?.MasterTable;
            var owner = master?.GetOwnerField()?.FieldName;
            if (!string.IsNullOrEmpty(owner)) { return owner; }
            var dept = master?.GetDeptField()?.FieldName;
            if (!string.IsNullOrEmpty(dept)) { return dept; }
            if (master?.Fields != null && master.Fields.Count > 0)
            {
                return master.Fields[0].FieldName;
            }
            return "sys_rowid";
        }

        private static List<object> OwnerIdentities(SessionInfo session)
        {
            var ids = new List<object>(2);
            if (session.UserRowId != Guid.Empty) { ids.Add(session.UserRowId); }
            if (session.EmployeeRowId != Guid.Empty) { ids.Add(session.EmployeeRowId); }
            return ids;
        }
    }
}
