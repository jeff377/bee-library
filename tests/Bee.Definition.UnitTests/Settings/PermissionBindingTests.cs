using System.ComponentModel;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Forms;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Settings
{
    /// <summary>
    /// FormSchema.PermissionModelId / FormField.ScopeRole 與 PermissionBindingValidator 的測試。
    /// </summary>
    public class PermissionBindingTests
    {
        /// <summary>
        /// 建立一份帶權限綁定的示範 FormSchema（PO001 → PurchaseOrder，主表標 Owner/Dept 欄）。
        /// </summary>
        private static FormSchema BuildForm(string modelId = "PurchaseOrder")
        {
            var schema = new FormSchema("PO001", "採購單建立")
            {
                CategoryId = "company",
                PermissionModelId = modelId,
            };
            var table = schema.Tables!.Add("PO001", "採購單");
            var buyer = table.Fields!.Add("buyer_rowid", "採購員", FieldDbType.Guid);
            buyer.ScopeRole = ScopeRole.Owner;
            var dept = table.Fields!.Add("dept_rowid", "部門", FieldDbType.Guid);
            dept.ScopeRole = ScopeRole.Dept;
            return schema;
        }

        private static PermissionModels BuildRegistry()
        {
            var models = new PermissionModels();
            var po = models.Models!.Add("PurchaseOrder", "採購單");
            po.Rules!.Add(PermissionAction.Read, ScopeStrategy.DeptAndSub);
            return models;
        }

        [Fact]
        [DisplayName("FormSchema.PermissionModelId 應透過 XmlAttribute 序列化往返")]
        public void FormSchema_PermissionModelId_RoundTrips()
        {
            var schema = BuildForm();

            var xml = XmlCodec.Serialize(schema);
            var restored = XmlCodec.Deserialize<FormSchema>(xml);

            Assert.Contains("PermissionModelId=\"PurchaseOrder\"", xml);
            Assert.NotNull(restored);
            Assert.Equal("PurchaseOrder", restored!.PermissionModelId);
        }

        [Fact]
        [DisplayName("FormField.ScopeRole 應透過 XmlAttribute 序列化往返")]
        public void FormField_ScopeRole_RoundTrips()
        {
            var schema = BuildForm();

            var xml = XmlCodec.Serialize(schema);
            var restored = XmlCodec.Deserialize<FormSchema>(xml);

            Assert.Contains("ScopeRole=\"Owner\"", xml);
            var table = restored!.Tables!["PO001"];
            Assert.Equal(ScopeRole.Owner, table.Fields!["buyer_rowid"].ScopeRole);
            Assert.Equal(ScopeRole.Dept, table.Fields!["dept_rowid"].ScopeRole);
        }

        [Fact]
        [DisplayName("FormField.ScopeRole 為 None 時 XML 不應輸出該屬性")]
        public void FormField_ScopeRoleNone_OmittedFromXml()
        {
            var schema = new FormSchema("PO001", "採購單") { CategoryId = "company" };
            var table = schema.Tables!.Add("PO001", "採購單");
            table.Fields!.Add("sys_id", "單號", FieldDbType.String);

            var xml = XmlCodec.Serialize(schema);

            Assert.DoesNotContain("ScopeRole=", xml);
        }

        [Fact]
        [DisplayName("FormSchema.Clone 應保留 PermissionModelId")]
        public void FormSchema_Clone_PreservesPermissionModelId()
        {
            var schema = BuildForm();

            var clone = schema.Clone();

            Assert.Equal("PurchaseOrder", clone.PermissionModelId);
        }

        [Fact]
        [DisplayName("FormField.Clone 應保留 ScopeRole")]
        public void FormField_Clone_PreservesScopeRole()
        {
            var schema = BuildForm();

            var clone = schema.Clone();

            Assert.Equal(ScopeRole.Owner, clone.Tables!["PO001"].Fields!["buyer_rowid"].ScopeRole);
        }

        [Fact]
        [DisplayName("Validator 合法綁定應回傳空清單")]
        public void Validate_ValidBinding_ReturnsEmpty()
        {
            var schemas = new FormSchema[] { BuildForm() };

            var errors = PermissionBindingValidator.Validate(schemas, BuildRegistry());

            Assert.Empty(errors);
        }

        [Fact]
        [DisplayName("Validator PermissionModelId 不存在應回報錯誤")]
        public void Validate_UnknownModelId_ReturnsError()
        {
            var schemas = new FormSchema[] { BuildForm("NoSuchModel") };

            var errors = PermissionBindingValidator.Validate(schemas, BuildRegistry());

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("NoSuchModel"));
        }

        [Fact]
        [DisplayName("Validator 主表多個 Owner 欄應通過（OR 聯集，不再限制單欄）")]
        public void Validate_MultipleOwnerColumns_Allowed()
        {
            var schema = BuildForm();
            schema.Tables!["PO001"].Fields!.Add("creator_rowid", "建立者", FieldDbType.Guid).ScopeRole = ScopeRole.Owner;

            var errors = PermissionBindingValidator.Validate([schema], BuildRegistry());

            Assert.Empty(errors);
        }

        [Fact]
        [DisplayName("Validator 主表多個 Dept 欄應通過（調職單調出/調入部門）")]
        public void Validate_MultipleDeptColumns_Allowed()
        {
            var schema = BuildForm();
            schema.Tables!["PO001"].Fields!.Add("to_dept_rowid", "調入部門", FieldDbType.Guid).ScopeRole = ScopeRole.Dept;

            var errors = PermissionBindingValidator.Validate([schema], BuildRegistry());

            Assert.Empty(errors);
        }

        [Fact]
        [DisplayName("Validator 明細表標 ScopeRole 應回報錯誤（record scope 僅限主表）")]
        public void Validate_DetailScopeRole_ReturnsError()
        {
            var schema = BuildForm();
            // 明細表（非主表）標 ScopeRole → 違規：scope 僅主表
            var detail = schema.Tables!.Add("PO001_Item", "採購單明細");
            var item = detail.Fields!.Add("owner_rowid", "擁有者", FieldDbType.Guid);
            item.ScopeRole = ScopeRole.Owner;
            var schemas = new FormSchema[] { schema };

            var errors = PermissionBindingValidator.Validate(schemas, BuildRegistry());

            Assert.Contains(errors, e => e.Contains("detail table") && e.Contains("PO001_Item"));
        }

        [Fact]
        [DisplayName("FormField.SensitiveCategory 應透過 XmlAttribute 序列化往返")]
        public void FormField_SensitiveCategory_RoundTrips()
        {
            var schema = BuildForm();
            schema.Tables!["PO001"].Fields!.Add("total_cost", "成本", FieldDbType.Decimal).SensitiveCategory = SensitiveCategory.Cost;

            var xml = XmlCodec.Serialize(schema);
            var restored = XmlCodec.Deserialize<FormSchema>(xml);

            Assert.Contains("SensitiveCategory=\"Cost\"", xml);
            Assert.Equal(SensitiveCategory.Cost, restored!.Tables!["PO001"].Fields!["total_cost"].SensitiveCategory);
        }

        [Fact]
        [DisplayName("FormField.SensitiveCategory 為 None 時 XML 不應輸出該屬性")]
        public void FormField_SensitiveCategoryNone_OmittedFromXml()
        {
            var schema = new FormSchema("PO001", "採購單") { CategoryId = "company" };
            schema.Tables!.Add("PO001", "採購單").Fields!.Add("sys_id", "單號", FieldDbType.String);

            var xml = XmlCodec.Serialize(schema);

            Assert.DoesNotContain("SensitiveCategory=", xml);
        }

        [Fact]
        [DisplayName("FormField.Clone 應保留 SensitiveCategory")]
        public void FormField_Clone_PreservesSensitiveCategory()
        {
            var schema = BuildForm();
            schema.Tables!["PO001"].Fields!.Add("total_cost", "成本", FieldDbType.Decimal).SensitiveCategory = SensitiveCategory.Cost;

            var clone = schema.Clone();

            Assert.Equal(SensitiveCategory.Cost, clone.Tables!["PO001"].Fields!["total_cost"].SensitiveCategory);
        }

        [Fact]
        [DisplayName("Validator 敏感分類對應的 well-known model 不存在應回報錯誤")]
        public void Validate_SensitiveCategoryWithoutModel_ReturnsError()
        {
            var schema = BuildForm();
            schema.Tables!["PO001"].Fields!.Add("total_cost", "成本", FieldDbType.Decimal).SensitiveCategory = SensitiveCategory.Cost;
            var schemas = new FormSchema[] { schema };

            // BuildRegistry 只有 PurchaseOrder，沒有 well-known 的 Cost model → 應報錯
            var errors = PermissionBindingValidator.Validate(schemas, BuildRegistry());

            Assert.Contains(errors, e => e.Contains("Cost") && e.Contains("total_cost"));
        }

        [Fact]
        [DisplayName("Validator 敏感分類 well-known model 存在時應通過")]
        public void Validate_SensitiveCategoryWithModel_ReturnsEmpty()
        {
            var schema = BuildForm();
            schema.Tables!["PO001"].Fields!.Add("total_cost", "成本", FieldDbType.Decimal).SensitiveCategory = SensitiveCategory.Cost;
            var schemas = new FormSchema[] { schema };

            var registry = BuildRegistry();
            registry.Models!.Add("Cost", "成本");

            var errors = PermissionBindingValidator.Validate(schemas, registry);

            Assert.Empty(errors);
        }
    }
}
