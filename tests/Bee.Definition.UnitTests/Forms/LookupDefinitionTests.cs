using System.ComponentModel;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// Lookup 定義層測試：FormField.DisplayField 與 FormSchema.LookupFields 的
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
            table.Fields!.Add(new FormField("sys_id", "單號", FieldDbType.String));
            table.Fields!.Add(new FormField("sys_name", "名稱", FieldDbType.String));

            var customerField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
                DisplayField = "ref_customer_name",
            };
            customerField.RelationFieldMappings!.Add("sys_id", "ref_customer_id");
            customerField.RelationFieldMappings!.Add("sys_name", "ref_customer_name");
            table.Fields!.Add(customerField);
            table.Fields!.Add(new FormField("ref_customer_id", "客戶代碼", FieldDbType.String, FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_customer_name", "客戶名稱", FieldDbType.String, FieldType.RelationField));
            return schema;
        }

        [Fact]
        [DisplayName("DisplayField 與 LookupFields 經 XML 序列化應正確還原")]
        public void XmlRoundTrip_RestoresDisplayFieldAndLookupFields()
        {
            var schema = BuildSchema();

            var xml = XmlCodec.Serialize(schema);
            var restored = XmlCodec.Deserialize<FormSchema>(xml);

            Assert.NotNull(restored);
            Assert.Equal("sys_id,sys_name,customer_grade", restored.LookupFields);
            Assert.Equal("ref_customer_name", restored.Tables![0].Fields!["customer_rowid"].DisplayField);
        }

        [Fact]
        [DisplayName("DisplayField 與 LookupFields 應出現在 JSON 輸出（JS 前端單向消費）")]
        public void JsonSerialize_CarriesDisplayFieldAndLookupFields()
        {
            var schema = BuildSchema();

            var json = JsonCodec.Serialize(schema);
            var restored = JsonCodec.Deserialize<FormSchema>(json);

            // The JSON wire is one-way for JS frontends: getter-only collections such as
            // `Tables` do not repopulate on deserialize, so field-level values are asserted
            // on the serialized payload and only top-level properties on the restored object.
            Assert.Contains("\"displayField\": \"ref_customer_name\"", json);
            Assert.NotNull(restored);
            Assert.Equal("sys_id,sys_name,customer_grade", restored.LookupFields);
        }

        [Fact]
        [DisplayName("未設定的 DisplayField 與 LookupFields 不應出現在 XML 輸出")]
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
        [DisplayName("Clone 應複製 DisplayField 與 LookupFields")]
        public void Clone_CopiesDisplayFieldAndLookupFields()
        {
            var schema = BuildSchema();

            var clone = schema.Clone();

            Assert.Equal("sys_id,sys_name,customer_grade", clone.LookupFields);
            Assert.Equal("ref_customer_name", clone.Tables![0].Fields!["customer_rowid"].DisplayField);
        }

        [Fact]
        [DisplayName("GetFormLayout relation 欄位應解析為 ButtonEdit 並帶 DisplayField")]
        public void GetFormLayout_RelationField_ResolvesButtonEditWithDisplayField()
        {
            var schema = BuildSchema();

            var layout = schema.GetFormLayout();
            var field = layout.Sections![0].Fields!.First(f => f.FieldName == "customer_rowid");

            Assert.Equal(ControlType.ButtonEdit, field.ControlType);
            Assert.Equal("ref_customer_name", field.DisplayField);
        }
    }
}
