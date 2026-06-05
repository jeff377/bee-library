using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// PermissionModels / PermissionModel / PermissionRule 權限定義類別的測試。
    /// </summary>
    public class PermissionModelsDataTests
    {
        /// <summary>
        /// 建立一份示範權限定義（PurchaseOrder / Vendor / Requisition），對應 plan 的測試標的。
        /// </summary>
        private static PermissionModels BuildSample()
        {
            var models = new PermissionModels();

            var po = models.Models!.Add("PurchaseOrder", "採購單");
            po.Rules!.Add(PermissionActions.Read, ScopeStrategy.DeptAndSub);
            po.Rules!.Add(PermissionActions.Create, ScopeStrategy.All);
            po.Rules!.Add(PermissionActions.Update, ScopeStrategy.Own);
            po.Rules!.Add(PermissionActions.Delete, ScopeStrategy.Own);
            po.Rules!.Add(PermissionActions.Print);
            po.Rules!.Add(PermissionActions.Export);

            var vendor = models.Models!.Add("Vendor", "廠商");
            vendor.Rules!.Add(PermissionActions.Read, ScopeStrategy.All);
            vendor.Rules!.Add(PermissionActions.Update, ScopeStrategy.Own);

            var req = models.Models!.Add("Requisition", "請購單");
            req.Rules!.Add(PermissionActions.Read, ScopeStrategy.DeptAndSub);
            req.Rules!.Add(PermissionActions.Create, ScopeStrategy.All);

            return models;
        }

        [Fact]
        [DisplayName("PermissionModel 帶參數建構子應設定 ModelId 與 DisplayName")]
        public void PermissionModel_ParameterizedConstructor_SetsProperties()
        {
            var model = new PermissionModel("PurchaseOrder", "採購單");

            Assert.Equal("PurchaseOrder", model.ModelId);
            Assert.Equal("採購單", model.DisplayName);
            Assert.Equal("PurchaseOrder", model.Key);
        }

        [Fact]
        [DisplayName("PermissionModel.ToString 應回傳 \"ModelId - DisplayName\"")]
        public void PermissionModel_ToString_ReturnsFormatted()
        {
            var model = new PermissionModel("PurchaseOrder", "採購單");

            Assert.Equal("PurchaseOrder - 採購單", model.ToString());
        }

        [Fact]
        [DisplayName("PermissionRule Action 應同時設定為集合 Key")]
        public void PermissionRule_Action_SetsAsKey()
        {
            var rule = new PermissionRule(PermissionActions.Update, ScopeStrategy.Own);

            Assert.Equal(PermissionActions.Update, rule.Action);
            Assert.Equal(ScopeStrategy.Own, rule.Scope);
            Assert.Equal("Update", rule.Key);
        }

        [Fact]
        [DisplayName("PermissionRule 預設 Scope 應為 Inherit")]
        public void PermissionRule_DefaultScope_IsInherit()
        {
            var rule = new PermissionRule(PermissionActions.Print);

            Assert.Equal(ScopeStrategy.Inherit, rule.Scope);
        }

        [Fact]
        [DisplayName("PermissionRuleCollection 以 Action 為鍵可索引")]
        public void PermissionRuleCollection_IndexedByAction()
        {
            var model = new PermissionModel("PurchaseOrder", "採購單");
            model.Rules!.Add(PermissionActions.Read, ScopeStrategy.DeptAndSub);

            Assert.Equal(ScopeStrategy.DeptAndSub, model.Rules!["Read"].Scope);
        }

        [Fact]
        [DisplayName("egress(Print) 未設 scope 時 XML 不應輸出 Scope 屬性")]
        public void PermissionRule_EgressInheritScope_OmittedFromXml()
        {
            var models = new PermissionModels();
            var po = models.Models!.Add("PurchaseOrder", "採購單");
            po.Rules!.Add(PermissionActions.Print);

            var xml = XmlCodec.Serialize(models);

            Assert.Contains("Action=\"Print\"", xml);
            Assert.DoesNotContain("Scope=\"Inherit\"", xml);
        }

        [Fact]
        [DisplayName("有設 scope 的 action XML 應輸出 Scope 屬性")]
        public void PermissionRule_ExplicitScope_WrittenToXml()
        {
            var models = new PermissionModels();
            var po = models.Models!.Add("PurchaseOrder", "採購單");
            po.Rules!.Add(PermissionActions.Update, ScopeStrategy.Own);

            var xml = XmlCodec.Serialize(models);

            Assert.Contains("Scope=\"Own\"", xml);
        }

        [Fact]
        [DisplayName("PermissionModels 序列化往返應保留 model / rule / scope")]
        public void PermissionModels_RoundTripsThroughXml()
        {
            var models = BuildSample();

            var xml = XmlCodec.Serialize(models);
            var restored = XmlCodec.Deserialize<PermissionModels>(xml);

            Assert.NotNull(restored);
            Assert.Equal(3, restored!.Models!.Count);

            var po = restored.Models!["PurchaseOrder"];
            Assert.Equal("採購單", po.DisplayName);
            Assert.Equal(ScopeStrategy.DeptAndSub, po.Rules!["Read"].Scope);
            Assert.Equal(ScopeStrategy.All, po.Rules!["Create"].Scope);
            Assert.Equal(ScopeStrategy.Own, po.Rules!["Update"].Scope);
            Assert.Equal(ScopeStrategy.Inherit, po.Rules!["Print"].Scope);
            Assert.Equal(PermissionActions.Export, po.Rules!["Export"].Action);

            Assert.Equal(ScopeStrategy.All, restored.Models!["Vendor"].Rules!["Read"].Scope);
        }

        [Fact]
        [DisplayName("Validate 合法 registry 應回傳空清單")]
        public void Validate_ValidRegistry_ReturnsEmpty()
        {
            var models = BuildSample();

            var errors = models.Validate();

            Assert.Empty(errors);
        }

        [Fact]
        [DisplayName("Validate egress 設明確 scope 應回報錯誤")]
        public void Validate_EgressWithScope_ReturnsError()
        {
            var models = new PermissionModels();
            var po = models.Models!.Add("PurchaseOrder", "採購單");
            po.Rules!.Add(PermissionActions.Print, ScopeStrategy.Own);

            var errors = models.Validate();

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("Print"));
        }
    }
}
