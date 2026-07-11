using System.ComponentModel;
using Bee.Business.Permission;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Organization;
using Bee.Definition.Settings;

namespace Bee.Business.UnitTests.Permission
{
    /// <summary>
    /// <see cref="ScopeResolver"/> 補洞覆蓋測試：建構子 null 防護、fail-closed 各入口（session null /
    /// snapshot null / 無 grant）、Inherit 解析（model 預設 Inherit → 回退 Read 預設 / model 未註冊 →
    /// All）、Dept/DeptAndSub 的空部門與空子樹提早返回、DenyAll 的 AnyMasterFieldName 各分支。
    /// </summary>
    public class ScopeResolverCoverageTests
    {
        private const string Model = "PurchaseOrder";
        private static readonly Guid s_user = Guid.NewGuid();
        private static readonly Guid s_employee = Guid.NewGuid();
        private static readonly Guid s_dept = Guid.NewGuid();

        // ---- builders ----

        private static SessionInfo Session(Guid user, Guid employee, Guid dept, params string[] roles)
            => new()
            {
                AccessToken = Guid.NewGuid(),
                CompanyId = "C001",
                Roles = roles.ToList(),
                UserRowId = user,
                EmployeeRowId = employee,
                DeptRowId = dept,
            };

        private static FormSchema Schema(bool owner = true, bool dept = true)
        {
            var schema = new FormSchema { ProgId = "PO001", PermissionModelId = Model };
            var table = new FormTable("PO001", "採購單");
            table.Fields!.Add(new FormField { FieldName = "sys_rowid" });
            if (owner) { table.Fields.Add(new FormField { FieldName = "buyer_rowid", ScopeRole = ScopeRole.Owner }); }
            if (dept) { table.Fields.Add(new FormField { FieldName = "dept_rowid", ScopeRole = ScopeRole.Dept }); }
            schema.Tables!.Add(table);
            return schema;
        }

        // A master table with no fields at all → AnyMasterFieldName falls through to "sys_rowid".
        private static FormSchema EmptyFieldSchema()
        {
            var schema = new FormSchema { ProgId = "PO001", PermissionModelId = Model };
            schema.Tables!.Add(new FormTable("PO001", "空表"));
            return schema;
        }

        private static DepartmentTree DeptTree()
            => new("C001",
            [
                new DepartmentRow(s_dept, "HQ", "總公司", Guid.Empty, Guid.Empty),
            ]);

        private static ScopeResolver Build(SessionInfo? session, List<RoleGrantRow>? grants,
            DepartmentTree? tree = null, PermissionModels? models = null, bool nullSnapshot = false)
        {
            var perms = nullSnapshot
                ? null
                : new CompanyRolePermissions("C001", grants ?? [], []);
            return new ScopeResolver(
                new CovSessionService(session),
                new CovRoleService(perms),
                new CovDeptService(tree),
                new FakeDefineAccess { PermissionModels = models });
        }

        // ---- constructor null guards ----

        [Fact]
        [DisplayName("建構子任一相依為 null 應丟 ArgumentNullException")]
        public void Ctor_NullDependencies_Throws()
        {
            var session = new CovSessionService(null);
            var role = new CovRoleService(null);
            var dept = new CovDeptService(null);
            var define = new FakeDefineAccess();

            Assert.Throws<ArgumentNullException>(() => new ScopeResolver(null!, role, dept, define));
            Assert.Throws<ArgumentNullException>(() => new ScopeResolver(session, null!, dept, define));
            Assert.Throws<ArgumentNullException>(() => new ScopeResolver(session, role, null!, define));
            Assert.Throws<ArgumentNullException>(() => new ScopeResolver(session, role, dept, null!));
        }

        // ---- fail-closed entry points ----

        [Fact]
        [DisplayName("session 不存在（null）→ DenyAll（owner 欄）")]
        public void ResolveFilter_NoSession_DeniesAll()
        {
            var resolver = Build(session: null, grants: []);

            var node = resolver.ResolveFilter(Guid.NewGuid(), Model, PermissionAction.Read, Schema());

            AssertDenyAll(node!, "buyer_rowid");
        }

        [Fact]
        [DisplayName("company 權限快照為 null → DenyAll")]
        public void ResolveFilter_NullSnapshot_DeniesAll()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var resolver = Build(session, grants: null, nullSnapshot: true);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Read, Schema());

            AssertDenyAll(node!, "buyer_rowid");
        }

        [Fact]
        [DisplayName("角色對該 model/action 無任何 grant → DenyAll")]
        public void ResolveFilter_NoGrant_DeniesAll()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var resolver = Build(session, grants: []);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Read, Schema());

            AssertDenyAll(node!, "buyer_rowid");
        }

        [Fact]
        [DisplayName("DenyAll：無 owner 欄但有 dept 欄 → 恆假掛在 dept 欄")]
        public void ResolveFilter_NoGrant_DeptFieldOnly_DeniesOnDept()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var resolver = Build(session, grants: []);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Read, Schema(owner: false));

            AssertDenyAll(node!, "dept_rowid");
        }

        [Fact]
        [DisplayName("DenyAll：master 無任何欄位 → 恆假掛在 sys_rowid（保底）")]
        public void ResolveFilter_NoGrant_EmptyMaster_DeniesOnSysRowId()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var resolver = Build(session, grants: []);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Read, EmptyFieldSchema());

            AssertDenyAll(node!, "sys_rowid");
        }

        // ---- Inherit resolution ----

        [Fact]
        [DisplayName("Inherit（Print）→ action 無預設 → 回退 model 的 Read 預設（Dept）")]
        public void ResolveFilter_InheritFallsBackToReadDefault()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var models = new PermissionModels();
            var model = models.Models!.Add(Model, "採購單");
            model.Rules!.Add(PermissionAction.Read, ScopeStrategy.Dept);

            var resolver = Build(session,
                [new("Buyer", Model, PermissionAction.Print, ScopeStrategy.Inherit)],
                tree: DeptTree(), models: models);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Print, Schema());

            // Print inherits Read=Dept → (dept=..) OR Own group.
            var group = Assert.IsType<FilterGroup>(node);
            Assert.Equal(LogicalOperator.Or, group.Operator);
        }

        [Fact]
        [DisplayName("Inherit（Print）→ model 有註冊但無任何 rule → 兩層皆 Inherit → All（不過濾）")]
        public void ResolveFilter_InheritNoRules_ResolvesAll()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var models = new PermissionModels();
            models.Models!.Add(Model, "採購單"); // 無 rules

            var resolver = Build(session,
                [new("Buyer", Model, PermissionAction.Print, ScopeStrategy.Inherit)],
                models: models);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Print, Schema());

            Assert.Null(node);
        }

        [Fact]
        [DisplayName("Inherit（Print）→ model 未註冊 → ModelDefault 回 Inherit → All（不過濾）")]
        public void ResolveFilter_InheritModelNotRegistered_ResolvesAll()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var models = new PermissionModels();
            models.Models!.Add("OtherModel", "他"); // 不含目標 model

            var resolver = Build(session,
                [new("Buyer", Model, PermissionAction.Print, ScopeStrategy.Inherit)],
                models: models);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Print, Schema());

            Assert.Null(node);
        }

        // ---- Dept / DeptAndSub early-return guards ----

        [Fact]
        [DisplayName("Dept scope 但 session 無部門（Empty）→ 只剩 Own 分支")]
        public void ResolveFilter_DeptScope_NoDept_OwnOnly()
        {
            var session = Session(s_user, s_employee, Guid.Empty, "Buyer");
            var resolver = Build(session, [new("Buyer", Model, PermissionAction.Read, ScopeStrategy.Dept)]);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Read, Schema());

            var c = Assert.IsType<FilterCondition>(node);
            Assert.Equal("buyer_rowid", c.FieldName);
            Assert.Equal(ComparisonOperator.In, c.Operator);
        }

        [Fact]
        [DisplayName("DeptAndSub 但 schema 無 dept 欄 → 子樹展開略過，只剩 Own")]
        public void ResolveFilter_DeptAndSub_NoDeptField_OwnOnly()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var resolver = Build(session,
                [new("Buyer", Model, PermissionAction.Read, ScopeStrategy.DeptAndSub)], tree: DeptTree());

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Read, Schema(dept: false));

            var c = Assert.IsType<FilterCondition>(node);
            Assert.Equal("buyer_rowid", c.FieldName);
        }

        [Fact]
        [DisplayName("DeptAndSub 但部門樹服務回 null（子樹空）→ 子樹展開略過，只剩 Own")]
        public void ResolveFilter_DeptAndSub_NullTree_OwnOnly()
        {
            var session = Session(s_user, s_employee, s_dept, "Buyer");
            var resolver = Build(session,
                [new("Buyer", Model, PermissionAction.Read, ScopeStrategy.DeptAndSub)], tree: null);

            var node = resolver.ResolveFilter(session.AccessToken, Model, PermissionAction.Read, Schema());

            var c = Assert.IsType<FilterCondition>(node);
            Assert.Equal("buyer_rowid", c.FieldName);
        }

        // ---- helpers ----

        private static void AssertDenyAll(FilterNode node, string expectedField)
        {
            var c = Assert.IsType<FilterCondition>(node);
            Assert.Equal(expectedField, c.FieldName);
            Assert.Equal(ComparisonOperator.In, c.Operator);
            Assert.Empty((IEnumerable<object>)c.Value!);
        }

        private sealed class CovSessionService : ISessionInfoService
        {
            private readonly SessionInfo? _session;
            public CovSessionService(SessionInfo? session) { _session = session; }
            public SessionInfo Get(Guid accessToken) => _session!;
            public void Set(SessionInfo sessionInfo) { }
            public void Remove(Guid accessToken) { }
        }

        private sealed class CovRoleService : IRolePermissionService
        {
            private readonly CompanyRolePermissions? _perms;
            public CovRoleService(CompanyRolePermissions? perms) { _perms = perms; }
            public CompanyRolePermissions? Get(string companyId) => _perms;
            public void Remove(string companyId) { }
        }

        private sealed class CovDeptService : IDepartmentTreeService
        {
            private readonly DepartmentTree? _tree;
            public CovDeptService(DepartmentTree? tree) { _tree = tree; }
            public DepartmentTree? Get(string companyId) => _tree;
            public void Remove(string companyId) { }
        }
    }
}
