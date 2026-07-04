using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Settings;
using Bee.UI.Core.Permissions;

namespace Bee.UI.Core.UnitTests.Permissions
{
    /// <summary>
    /// <see cref="ElementCapabilityResolver"/> 的純函式判定測試：命令 Can（any-of / 未綁 model /
    /// null 快照）、敏感欄位 Read/Update 兩階降級、Grid 動作交集。
    /// </summary>
    public class ElementCapabilityResolverTests
    {
        private static readonly ElementCapabilityResolver s_resolver = ElementCapabilityResolver.Default;

        // PO001 → PurchaseOrder，主表帶一個一般欄與一個敏感成本欄（SensitiveCategory=Cost）。
        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("PO001", "採購單") { PermissionModelId = "PurchaseOrder" };
            var table = schema.Tables!.Add("PO001", "採購單");
            table.Fields!.Add("sys_name", "單號", FieldDbType.String);
            table.Fields!.Add("total_cost", "成本", FieldDbType.Decimal).SensitiveCategory = SensitiveCategory.Cost;
            return schema;
        }

        private static Dictionary<string, PermissionAction> Caps(params (string model, PermissionAction action)[] entries)
        {
            var map = new Dictionary<string, PermissionAction>(StringComparer.Ordinal);
            foreach (var (model, action) in entries) { map[model] = action; }
            return map;
        }

        [Fact]
        [DisplayName("Can 有授予該 action 應允許")]
        public void Can_GrantedAction_ReturnsTrue()
        {
            var caps = Caps(("PurchaseOrder", PermissionAction.Create | PermissionAction.Read));

            Assert.True(s_resolver.Can(BuildSchema(), PermissionAction.Create, caps));
        }

        [Fact]
        [DisplayName("Can 未授予該 action 應拒絕")]
        public void Can_UngrantedAction_ReturnsFalse()
        {
            var caps = Caps(("PurchaseOrder", PermissionAction.Create | PermissionAction.Read));

            Assert.False(s_resolver.Can(BuildSchema(), PermissionAction.Delete, caps));
        }

        [Fact]
        [DisplayName("Can 複合旗標採 any-of：Save=Create|Update 只要有其一即允許")]
        public void Can_CombinedFlags_AnyOf()
        {
            var caps = Caps(("PurchaseOrder", PermissionAction.Update)); // 只有 Update

            Assert.True(s_resolver.Can(BuildSchema(), PermissionAction.Create | PermissionAction.Update, caps));
        }

        [Fact]
        [DisplayName("Can action 為 None（未綁定命令）一律允許")]
        public void Can_NoneAction_ReturnsTrue()
        {
            var caps = Caps(("PurchaseOrder", PermissionAction.None));

            Assert.True(s_resolver.Can(BuildSchema(), PermissionAction.None, caps));
        }

        [Fact]
        [DisplayName("Can 表單未宣告 PermissionModelId 一律允許")]
        public void Can_NoPermissionModel_ReturnsTrue()
        {
            var schema = new FormSchema("PO001", "採購單"); // 無 PermissionModelId
            var caps = Caps(("PurchaseOrder", PermissionAction.None));

            Assert.True(s_resolver.Can(schema, PermissionAction.Delete, caps));
        }

        [Fact]
        [DisplayName("Can 快照為 null（enforcement 未啟用）一律允許")]
        public void Can_NullSnapshot_ReturnsTrue()
        {
            Assert.True(s_resolver.Can(BuildSchema(), PermissionAction.Delete, capabilities: null));
        }

        [Fact]
        [DisplayName("ResolveField 非敏感欄（None）不控管")]
        public void ResolveField_NonSensitive_Allowed()
        {
            var caps = Caps(("Cost", PermissionAction.None));

            var cap = s_resolver.ResolveField(BuildSchema(), "sys_name", tableName: "", caps);

            Assert.Equal(FieldCapability.Allowed, cap);
        }

        [Fact]
        [DisplayName("ResolveField 敏感欄無 Read 應隱藏")]
        public void ResolveField_SensitiveNoRead_Hidden()
        {
            var caps = Caps(("Cost", PermissionAction.None)); // Cost 無任何權限

            var cap = s_resolver.ResolveField(BuildSchema(), "total_cost", tableName: "", caps);

            Assert.False(cap.Visible);
        }

        [Fact]
        [DisplayName("ResolveField 敏感欄有 Read 無 Update 應唯讀")]
        public void ResolveField_SensitiveReadNoUpdate_ReadOnly()
        {
            var caps = Caps(("Cost", PermissionAction.Read));

            var cap = s_resolver.ResolveField(BuildSchema(), "total_cost", tableName: "", caps);

            Assert.True(cap.Visible);
            Assert.True(cap.ReadOnly);
        }

        [Fact]
        [DisplayName("ResolveField 敏感欄有 Read+Update 不降級")]
        public void ResolveField_SensitiveReadUpdate_Allowed()
        {
            var caps = Caps(("Cost", PermissionAction.Read | PermissionAction.Update));

            var cap = s_resolver.ResolveField(BuildSchema(), "total_cost", tableName: "", caps);

            Assert.Equal(FieldCapability.Allowed, cap);
        }

        [Fact]
        [DisplayName("ResolveField 快照為 null 敏感欄也不控管")]
        public void ResolveField_NullSnapshot_Allowed()
        {
            var cap = s_resolver.ResolveField(BuildSchema(), "total_cost", tableName: "", capabilities: null);

            Assert.Equal(FieldCapability.Allowed, cap);
        }
    }
}
