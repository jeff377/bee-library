using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
using Bee.ObjectCaching.Services;

namespace Bee.ObjectCaching.UnitTests.Services
{
    /// <summary>
    /// AuthorizationService.Can 的層一判定串接測試（以 fake session / role-permission service
    /// 隔離,驗證空檢查 + 多角色 OR 合併 + HasFlag）。
    /// </summary>
    public class AuthorizationServiceTests
    {
        private static readonly Guid s_token = Guid.NewGuid();

        private static CompanyRolePermissions BuildPerms()
        {
            var grants = new List<RoleGrantRow>
            {
                new("Buyer", "PurchaseOrder", PermissionAction.Read, ScopeStrategy.All),
                new("Buyer", "PurchaseOrder", PermissionAction.Update, ScopeStrategy.All),
                new("Manager", "PurchaseOrder", PermissionAction.Delete, ScopeStrategy.All),
            };
            return new CompanyRolePermissions("C001", grants, []);
        }

        private static SessionInfo Session(string? companyId, params string[] roles)
            => new() { AccessToken = s_token, UserId = "001", CompanyId = companyId, Roles = roles.ToList() };

        private static AuthorizationService Create(SessionInfo? session, CompanyRolePermissions? perms)
            => new(new FakeSessionInfoService(session), new FakeRolePermissionService(perms));

        [Fact]
        [DisplayName("Can 已授予的 action 回 true")]
        public void Can_GrantedAction_ReturnsTrue()
        {
            var auth = Create(Session("C001", "Buyer"), BuildPerms());

            Assert.True(auth.Can(s_token, "PurchaseOrder", PermissionAction.Read));
        }

        [Fact]
        [DisplayName("Can 未授予的 action 回 false")]
        public void Can_UngrantedAction_ReturnsFalse()
        {
            var auth = Create(Session("C001", "Buyer"), BuildPerms());

            // Buyer 沒有 Delete（只有 Manager 有）
            Assert.False(auth.Can(s_token, "PurchaseOrder", PermissionAction.Delete));
        }

        [Fact]
        [DisplayName("Can 多角色對同 model 應 OR 合併後判定")]
        public void Can_MultiRole_OrMerges()
        {
            var auth = Create(Session("C001", "Buyer", "Manager"), BuildPerms());

            // Buyer(Read|Update) ∪ Manager(Delete) → Delete 通過
            Assert.True(auth.Can(s_token, "PurchaseOrder", PermissionAction.Delete));
        }

        [Fact]
        [DisplayName("Can 未進公司（CompanyId 為 null）回 false")]
        public void Can_NoCompany_ReturnsFalse()
        {
            var auth = Create(Session(null, "Buyer"), BuildPerms());

            Assert.False(auth.Can(s_token, "PurchaseOrder", PermissionAction.Read));
        }

        [Fact]
        [DisplayName("Can 無角色回 false")]
        public void Can_NoRoles_ReturnsFalse()
        {
            var auth = Create(Session("C001"), BuildPerms());

            Assert.False(auth.Can(s_token, "PurchaseOrder", PermissionAction.Read));
        }

        [Fact]
        [DisplayName("Can session 不存在回 false")]
        public void Can_NoSession_ReturnsFalse()
        {
            var auth = Create(null, BuildPerms());

            Assert.False(auth.Can(s_token, "PurchaseOrder", PermissionAction.Read));
        }

        private sealed class FakeSessionInfoService : ISessionInfoService
        {
            private readonly SessionInfo? _session;
            public FakeSessionInfoService(SessionInfo? session) { _session = session; }
            public SessionInfo Get(Guid accessToken) => _session!;
            public void Set(SessionInfo sessionInfo) { }
            public void Remove(Guid accessToken) { }
        }

        private sealed class FakeRolePermissionService : IRolePermissionService
        {
            private readonly CompanyRolePermissions? _perms;
            public FakeRolePermissionService(CompanyRolePermissions? perms) { _perms = perms; }
            public CompanyRolePermissions? Get(string companyId) => _perms;
            public void Remove(string companyId) { }
        }
    }
}
