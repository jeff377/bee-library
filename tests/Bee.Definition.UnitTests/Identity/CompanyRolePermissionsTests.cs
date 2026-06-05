using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Identity
{
    /// <summary>
    /// CompanyRolePermissions 的層一判定邏輯測試（多角色 OR 合併、未授予、user→role）。
    /// </summary>
    public class CompanyRolePermissionsTests
    {
        private static readonly string s_user = "U001";
        private static readonly string[] s_buyer = { "Buyer" };
        private static readonly string[] s_buyerManager = { "Buyer", "Manager" };

        private static CompanyRolePermissions Build()
        {
            var grants = new List<RoleGrantRow>
            {
                new("Buyer", "PurchaseOrder", PermissionAction.Read, ScopeStrategy.Dept),
                new("Buyer", "PurchaseOrder", PermissionAction.Update, ScopeStrategy.Own),
                new("Buyer", "Vendor", PermissionAction.Read, ScopeStrategy.All),
                new("Manager", "PurchaseOrder", PermissionAction.Read, ScopeStrategy.All),
                new("Manager", "PurchaseOrder", PermissionAction.Delete, ScopeStrategy.Inherit),
            };
            var userRoles = new List<UserRoleRow>
            {
                new(s_user, "Buyer"),
                new(s_user, "Manager"),
            };
            return new CompanyRolePermissions("C001", grants, userRoles);
        }

        [Fact]
        [DisplayName("GetAllowed 單一角色回傳該角色的 action mask")]
        public void GetAllowed_SingleRole_ReturnsMask()
        {
            var perms = Build();

            var allowed = perms.GetAllowed(s_buyer, "PurchaseOrder");

            Assert.Equal(PermissionAction.Read | PermissionAction.Update, allowed);
        }

        [Fact]
        [DisplayName("GetAllowed 多角色對同 model 應 OR 合併（能力累加）")]
        public void GetAllowed_MultiRole_OrMerges()
        {
            var perms = Build();

            // Buyer(Read|Update) ∪ Manager(Delete) on PurchaseOrder
            var allowed = perms.GetAllowed(s_buyerManager, "PurchaseOrder");

            Assert.Equal(PermissionAction.Read | PermissionAction.Update | PermissionAction.Delete, allowed);
            Assert.True(allowed.HasFlag(PermissionAction.Delete));
        }

        [Fact]
        [DisplayName("GetAllowed 未授予的 model 回傳 None")]
        public void GetAllowed_UnauthorizedModel_ReturnsNone()
        {
            var perms = Build();

            var allowed = perms.GetAllowed(s_buyerManager, "Requisition");

            Assert.Equal(PermissionAction.None, allowed);
            Assert.False(allowed.HasFlag(PermissionAction.Read));
        }

        [Fact]
        [DisplayName("GetAllowed 只算使用者持有的角色，排除未持有角色的授權")]
        public void GetAllowed_OnlyHeldRoles_ExcludesOthers()
        {
            var perms = Build();

            // 只持有 Buyer → 不含 Manager 的 Delete
            var allowed = perms.GetAllowed(s_buyer, "PurchaseOrder");

            Assert.False(allowed.HasFlag(PermissionAction.Delete));
        }

        [Fact]
        [DisplayName("GetEffectiveScopes 單一角色回傳該 (model, action) 的 scope")]
        public void GetEffectiveScopes_SingleRole_ReturnsScope()
        {
            var perms = Build();

            var scopes = perms.GetEffectiveScopes(s_buyer, "PurchaseOrder", PermissionAction.Read);

            Assert.Equal(new[] { ScopeStrategy.Dept }, scopes);
        }

        [Fact]
        [DisplayName("GetEffectiveScopes 多角色對同 (model, action) 各回一個 scope（供合併）")]
        public void GetEffectiveScopes_MultiRole_ReturnsEach()
        {
            var perms = Build();

            // Buyer.Read=Dept、Manager.Read=All → 兩個 scope 待合併（resolver 端「任一 All 不過濾」）
            var scopes = perms.GetEffectiveScopes(s_buyerManager, "PurchaseOrder", PermissionAction.Read);

            Assert.Equal(2, scopes.Count);
            Assert.Contains(ScopeStrategy.Dept, scopes);
            Assert.Contains(ScopeStrategy.All, scopes);
        }

        [Fact]
        [DisplayName("GetEffectiveScopes 同角色不同 action 可有不同 scope（看 vs 改範圍不同）")]
        public void GetEffectiveScopes_PerAction_Differs()
        {
            var perms = Build();

            // Buyer 對 PurchaseOrder：Read=Dept（可看部門）、Update=Own（只改自己的）
            Assert.Equal(new[] { ScopeStrategy.Dept }, perms.GetEffectiveScopes(s_buyer, "PurchaseOrder", PermissionAction.Read));
            Assert.Equal(new[] { ScopeStrategy.Own }, perms.GetEffectiveScopes(s_buyer, "PurchaseOrder", PermissionAction.Update));
        }

        [Fact]
        [DisplayName("GetEffectiveScopes 未授予該 action 回傳空")]
        public void GetEffectiveScopes_Ungranted_ReturnsEmpty()
        {
            var perms = Build();

            // Buyer 沒有 PurchaseOrder.Delete
            Assert.Empty(perms.GetEffectiveScopes(s_buyer, "PurchaseOrder", PermissionAction.Delete));
        }

        [Fact]
        [DisplayName("GetUserRoleIds 回傳該使用者被指派的角色")]
        public void GetUserRoleIds_ReturnsAssignedRoles()
        {
            var perms = Build();

            var roles = perms.GetUserRoleIds(s_user);

            Assert.Equal(2, roles.Count);
            Assert.Contains("Buyer", roles);
            Assert.Contains("Manager", roles);
        }

        [Fact]
        [DisplayName("GetUserRoleIds 對未指派的使用者回傳空")]
        public void GetUserRoleIds_UnknownUser_ReturnsEmpty()
        {
            var perms = Build();

            var roles = perms.GetUserRoleIds("UNKNOWN");

            Assert.Empty(roles);
        }
    }
}
