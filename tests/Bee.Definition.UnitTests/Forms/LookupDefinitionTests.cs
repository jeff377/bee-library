using System.ComponentModel;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// Lookup 定義層測試：FormField.DisplayFields 與 FormSchema.LookupFields 的
    /// 序列化 round-trip、Clone 同步，以及 FormLayoutGenerator 對 relation 欄位的解析。
    /// </summary>
    public class LookupDefinitionTests
    {
        /// <summary>
        /// 建立含 relation 欄位的測試 schema（鏡照 tests/Define Project 形狀）。
        /// </summary>
        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Order", "訂單")
            {
                CategoryId = "company",
                ListFields = "sys_id,sys_name",
                LookupFields = "sys_id,sys_name,customer_grade",
            };
            var table = schema.Tables!.Add("Order", "訂單");
            table.Fields!.Add(new FormField("sys_rowid", "唯一識別", FieldDbType.Guid));
            table.Fields!.Add(new FormField("sys_id", "單號", FieldDbType.String));
            table.Fields!.Add(new FormField("sys_name", "名稱", FieldDbType.String));

            var customerField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
                DisplayFields = "ref_customer_name",
            };
            customerField.RelationFieldMappings!.Add("sys_id", "ref_customer_id");
            customerField.RelationFieldMappings!.Add("sys_name", "ref_customer_name");
            table.Fields!.Add(customerField);
            table.Fields!.Add(new FormField("ref_customer_id", "客戶代碼", FieldDbType.String, FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_customer_name", "客戶名稱", FieldDbType.String, FieldType.RelationField));
            return schema;
        }

        [Fact]
        [DisplayName("DisplayFields 與 LookupFields 經 XML 序列化應正確還原")]
        public void XmlRoundTrip_RestoresDisplayFieldAndLookupFields()
        {
            var schema = BuildSchema();

            var xml = XmlCodec.Serialize(schema);
            var restored = XmlCodec.Deserialize<FormSchema>(xml);

            Assert.NotNull(restored);
            Assert.Equal("sys_id,sys_name,customer_grade", restored.LookupFields);
            Assert.Equal("ref_customer_name", restored.Tables![0].Fields!["customer_rowid"].DisplayFields);
        }

        [Fact]
        [DisplayName("DisplayFields 與 LookupFields 應出現在 JSON 輸出（JS 前端單向消費）")]
        public void JsonSerialize_CarriesDisplayFieldAndLookupFields()
        {
            var schema = BuildSchema();

            var json = JsonCodec.Serialize(schema);
            var restored = JsonCodec.Deserialize<FormSchema>(json);

            // The JSON wire is one-way for JS frontends: getter-only collections such as
            // `Tables` do not repopulate on deserialize, so field-level values are asserted
            // on the serialized payload and only top-level properties on the restored object.
            Assert.Contains("\"displayFields\": \"ref_customer_name\"", json);
            Assert.NotNull(restored);
            Assert.Equal("sys_id,sys_name,customer_grade", restored.LookupFields);
        }

        [Fact]
        [DisplayName("未設定的 DisplayFields 與 LookupFields 不應出現在 XML 輸出")]
        public void XmlSerialize_DefaultsOmitted()
        {
            var schema = new FormSchema("Customer", "客戶") { CategoryId = "company" };
            var table = schema.Tables!.Add("Customer", "客戶");
            table.Fields!.Add(new FormField("sys_id", "代碼", FieldDbType.String));

            var xml = XmlCodec.Serialize(schema);

            Assert.DoesNotContain("LookupFields", xml);
            Assert.DoesNotContain("DisplayField", xml);
        }

        [Fact]
        [DisplayName("Clone 應複製 DisplayFields 與 LookupFields")]
        public void Clone_CopiesDisplayFieldAndLookupFields()
        {
            var schema = BuildSchema();

            var clone = schema.Clone();

            Assert.Equal("sys_id,sys_name,customer_grade", clone.LookupFields);
            Assert.Equal("ref_customer_name", clone.Tables![0].Fields!["customer_rowid"].DisplayFields);
        }

        [Fact]
        [DisplayName("GetLookupFields 依宣告順序回傳並略過 master 不存在的欄位")]
        public void GetLookupFields_Declared_SkipsMissingFields()
        {
            var schema = BuildSchema();

            var fields = schema.GetLookupFields();

            // Declared "sys_id,sys_name,customer_grade" — customer_grade is not on the master table.
            Assert.Equal(2, fields.Count);
            Assert.Equal("sys_id", fields[0].FieldName);
            Assert.Equal("sys_name", fields[1].FieldName);
        }

        [Fact]
        [DisplayName("GetLookupFields 未宣告時應預設取 sys_id 與 sys_name")]
        public void GetLookupFields_NotDeclared_DefaultsToIdAndName()
        {
            var schema = BuildSchema();
            schema.LookupFields = string.Empty;

            var fields = schema.GetLookupFields();

            Assert.Equal(2, fields.Count);
            Assert.Equal("sys_id", fields[0].FieldName);
            Assert.Equal("sys_name", fields[1].FieldName);
        }

        [Fact]
        [DisplayName("GetLookupFields 宣告含 sys_rowid 應排除（呼叫端固定 prepend）")]
        public void GetLookupFields_DeclaredRowId_Excluded()
        {
            var schema = BuildSchema();
            schema.LookupFields = "sys_rowid,sys_id";

            var fields = schema.GetLookupFields();

            Assert.Single(fields);
            Assert.Equal("sys_id", fields[0].FieldName);
        }

        [Fact]
        [DisplayName("GetLookupFields master 無 sys_name 時預設集只回傳 sys_id")]
        public void GetLookupFields_MasterWithoutSysName_DefaultsToIdOnly()
        {
            var schema = new FormSchema("Unit", "單位") { CategoryId = "common" };
            var table = schema.Tables!.Add("Unit", "單位");
            table.Fields!.Add(new FormField("sys_id", "代碼", FieldDbType.String));

            var fields = schema.GetLookupFields();

            Assert.Single(fields);
            Assert.Equal("sys_id", fields[0].FieldName);
        }

        [Fact]
        [DisplayName("GetLookupLayout 應含 lookup 欄位與隱藏 sys_rowid、不允許編輯動作")]
        public void GetLookupLayout_BuildsSelectionOnlyGrid()
        {
            var schema = BuildSchema();

            var layout = schema.GetLookupLayout();

            Assert.Equal(GridControlAllowActions.None, layout.AllowActions);
            Assert.Equal(3, layout.Columns!.Count);
            Assert.Equal("sys_id", layout.Columns[0].FieldName);
            Assert.Equal("sys_name", layout.Columns[1].FieldName);
            Assert.Equal("sys_rowid", layout.Columns[2].FieldName);
            Assert.False(layout.Columns[2].Visible);
        }

        [Fact]
        [DisplayName("GetFormLayout relation 欄位應解析為 ButtonEdit 並帶 DisplayFields")]
        public void GetFormLayout_RelationField_ResolvesButtonEditWithDisplayField()
        {
            var schema = BuildSchema();

            var layout = schema.GetFormLayout();
            var field = layout.Sections![0].Fields!.First(f => f.FieldName == "customer_rowid");

            Assert.Equal(ControlType.ButtonEdit, field.ControlType);
            Assert.Equal("ref_customer_name", field.DisplayFields);
        }

        [Fact]
        [DisplayName("GetFormLayout：relation 欄 Visible=false 仍產出 ButtonEdit；被 DisplayFields 涵蓋的欄位不另行產生")]
        public void GetFormLayout_RelationField_CoversDisplayFields()
        {
            // 直觀設定：rowid 的原始值（Guid）永遠不會被看到 → Visible=false；
            // ref 欄位是實際看到的值 → Visible=true。ButtonEdit 由 RelationProgId
            // 驅動產生並承載 DisplayFields，被涵蓋的欄位不再各自出現。
            var schema = BuildSchema();
            var customerField = schema.MasterTable!.Fields!["customer_rowid"];
            customerField.Visible = false;
            customerField.DisplayFields = string.Empty;  // 慣例：ref_customer_id + ref_customer_name

            var layout = schema.GetFormLayout();
            var fields = layout.Sections![0].Fields!;

            // relation 欄位仍產出（編輯入口）
            var lookup = fields.First(f => f.FieldName == "customer_rowid");
            Assert.Equal(ControlType.ButtonEdit, lookup.ControlType);
            Assert.Equal("ref_customer_id,ref_customer_name", lookup.DisplayFields);
            // 被複合顯示涵蓋的欄位不另行產生
            Assert.DoesNotContain(fields, f => f.FieldName == "ref_customer_id");
            Assert.DoesNotContain(fields, f => f.FieldName == "ref_customer_name");
        }

        [Fact]
        [DisplayName("GetFormLayout：未被 DisplayFields 涵蓋的 ref 欄位仍各自產出（不丟資訊）")]
        public void GetFormLayout_UncoveredRelationDisplayField_StillEmitted()
        {
            var schema = BuildSchema();
            var customerField = schema.MasterTable!.Fields!["customer_rowid"];
            // 顯式只顯示名稱 → ref_customer_id 未被涵蓋，應以獨立欄位產出
            customerField.DisplayFields = "ref_customer_name";

            var layout = schema.GetFormLayout();
            var fields = layout.Sections![0].Fields!;

            Assert.Contains(fields, f => f.FieldName == "ref_customer_id");
            Assert.DoesNotContain(fields, f => f.FieldName == "ref_customer_name");
        }

        [Fact]
        [DisplayName("GetFormLayout：明細表套用同一套涵蓋規則")]
        public void GetFormLayout_DetailGrid_CoversDisplayFields()
        {
            var schema = BuildSchema();
            var detail = schema.Tables!.Add("OrderLine", "訂單明細");
            detail.Fields!.Add(new FormField("sys_rowid", "唯一識別", FieldDbType.Guid));
            detail.Fields!.Add(new FormField("sys_master_rowid", "主檔識別", FieldDbType.Guid));
            var productField = new FormField("product_rowid", "商品", FieldDbType.Guid)
            {
                RelationProgId = "Product",
                Visible = false,
            };
            productField.RelationFieldMappings!.Add("sys_id", "ref_product_id");
            productField.RelationFieldMappings!.Add("sys_name", "ref_product_name");
            detail.Fields!.Add(productField);
            detail.Fields!.Add(new FormField("ref_product_id", "商品代碼", FieldDbType.String, FieldType.RelationField));
            detail.Fields!.Add(new FormField("ref_product_name", "商品名稱", FieldDbType.String, FieldType.RelationField));

            var layout = schema.GetFormLayout();
            var columns = layout.Details![0].Columns!;

            var lookup = columns.First(c => c.FieldName == "product_rowid");
            Assert.Equal(ControlType.ButtonEdit, lookup.ControlType);
            Assert.DoesNotContain(columns, c => c.FieldName == "ref_product_id");
            Assert.DoesNotContain(columns, c => c.FieldName == "ref_product_name");
        }
    }
}
