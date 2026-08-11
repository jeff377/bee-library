using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Identity
{
    /// <summary>
    /// <see cref="CompanyRolePermissions"/> 改為建構時預建索引後的行為回歸。
    /// </summary>
    /// <remarks>
    /// 這是純效能重構，**行為必須完全不變**，因此測的是語意而非速度：多角色 OR 合併、
    /// 未持有的角色不得計入、精確 action 比對、以及重複 grant 的合併。
    /// </remarks>
    public class CompanyRolePermissionsIndexTests
    {
        private static CompanyRolePermissions Build()
        {
            var grants = new List<RoleGrantRow>
            {
                new("admin",  "order", PermissionAction.Read,   ScopeStrategy.All),
                new("admin",  "order", PermissionAction.Update, ScopeStrategy.Own),
                new("clerk",  "order", PermissionAction.Read,   ScopeStrategy.Dept),
                new("clerk",  "item",  PermissionAction.Read,   ScopeStrategy.All),
                new("other",  "order", PermissionAction.Delete, ScopeStrategy.All),
            };
            var userRoles = new List<UserRoleRow>
            {
                new("u1", "admin"),
                new("u1", "clerk"),
                new("u2", "other"),
            };
            return new CompanyRolePermissions("C1", grants, userRoles);
        }

        [Fact]
        [DisplayName("GetAllowed 應 OR 合併多個角色在同一模型上的權限")]
        public void GetAllowed_MergesAcrossRoles()
        {
            var allowed = Build().GetAllowed(["admin", "clerk"], "order");

            Assert.Equal(PermissionAction.Read | PermissionAction.Update, allowed);
        }

        [Fact]
        [DisplayName("GetAllowed 不應計入使用者未持有的角色")]
        public void GetAllowed_IgnoresRolesNotHeld()
        {
            // "other" 在 order 上有 Delete，但使用者只持有 clerk —— 不得洩漏進來。
            var allowed = Build().GetAllowed(["clerk"], "order");

            Assert.Equal(PermissionAction.Read, allowed);
            Assert.False(allowed.HasFlag(PermissionAction.Delete));
        }

        [Fact]
        [DisplayName("GetAllowed 對無任何授權的模型應回傳 None")]
        public void GetAllowed_UnknownModel_ReturnsNone()
        {
            Assert.Equal(PermissionAction.None, Build().GetAllowed(["admin"], "nowhere"));
        }

        [Fact]
        [DisplayName("GetAllowedByModel 應回傳所持角色的逐模型合併結果")]
        public void GetAllowedByModel_MergesPerModel()
        {
            var byModel = Build().GetAllowedByModel(["admin", "clerk"]);

            Assert.Equal(2, byModel.Count);
            Assert.Equal(PermissionAction.Read | PermissionAction.Update, byModel["order"]);
            Assert.Equal(PermissionAction.Read, byModel["item"]);
        }

        [Fact]
        [DisplayName("GetEffectiveScopes 應以精確 action 比對，且涵蓋所有持有角色")]
        public void GetEffectiveScopes_MatchesActionExactly()
        {
            var scopes = Build().GetEffectiveScopes(["admin", "clerk"], "order", PermissionAction.Read);

            // admin 的 Read 是 All、clerk 的 Read 是 Dept；admin 的 Update(Own) 不得混入。
            Assert.Equal(2, scopes.Count);
            Assert.Contains(ScopeStrategy.All, scopes);
            Assert.Contains(ScopeStrategy.Dept, scopes);
            Assert.DoesNotContain(ScopeStrategy.Own, scopes);
        }

        [Fact]
        [DisplayName("GetUserRoleIds 應回傳該使用者的全部角色，其他使用者的不得混入")]
        public void GetUserRoleIds_ReturnsOnlyThatUsersRoles()
        {
            var roles = Build().GetUserRoleIds("u1");

            Assert.Equal(2, roles.Count);
            Assert.Contains("admin", roles);
            Assert.Contains("clerk", roles);
            Assert.DoesNotContain("other", roles);
        }

        [Fact]
        [DisplayName("GetUserRoleIds 對未知使用者應回傳空集合而非擲例外")]
        public void GetUserRoleIds_UnknownUser_ReturnsEmpty()
        {
            Assert.Empty(Build().GetUserRoleIds("nobody"));
        }

        [Fact]
        [DisplayName("同一 (角色, 模型) 的重複 grant 應合併為單一遮罩")]
        public void DuplicateGrants_AreMerged()
        {
            var grants = new List<RoleGrantRow>
            {
                new("r", "m", PermissionAction.Read,   ScopeStrategy.All),
                new("r", "m", PermissionAction.Delete, ScopeStrategy.All),
            };
            var perms = new CompanyRolePermissions("C1", grants, []);

            Assert.Equal(PermissionAction.Read | PermissionAction.Delete, perms.GetAllowed(["r"], "m"));
        }
    }
}
