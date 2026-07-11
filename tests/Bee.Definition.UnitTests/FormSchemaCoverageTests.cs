using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// <see cref="FormSchema"/> 補測：<see cref="FormSchema.FindField"/> 的主檔／具名表／欄位不存在分支、
    /// <see cref="FormSchema.GetLookupFields"/> 在無主檔時的早退，以及 <see cref="FormSchema.ToString"/>。
    /// </summary>
    public class FormSchemaCoverageTests
    {
        private static FormSchema BuildMasterDetailSchema()
        {
            var schema = new FormSchema("Order", "訂單") { CategoryId = "company" };
            var master = schema.Tables!.Add("Order", "訂單");
            master.Fields!.Add("sys_id", "編號", FieldDbType.String);
            master.Fields.Add("sys_name", "名稱", FieldDbType.String);

            var detail = schema.Tables!.Add("OrderLine", "明細");
            detail.Fields!.Add("qty", "數量", FieldDbType.Integer);
            return schema;
        }

        [Fact]
        [DisplayName("FindField：tableName 為空時解析主檔並回傳既有欄位")]
        public void FindField_EmptyTableName_ResolvesMasterField()
        {
            var schema = BuildMasterDetailSchema();

            var field = schema.FindField("sys_id");

            Assert.NotNull(field);
            Assert.Equal("sys_id", field!.FieldName);
        }

        [Fact]
        [DisplayName("FindField：指定既有具名表可回傳該表欄位")]
        public void FindField_NamedTablePresent_ResolvesField()
        {
            var schema = BuildMasterDetailSchema();

            var field = schema.FindField("qty", "OrderLine");

            Assert.NotNull(field);
            Assert.Equal("qty", field!.FieldName);
        }

        [Fact]
        [DisplayName("FindField：指定的具名表不存在時回傳 null")]
        public void FindField_NamedTableAbsent_ReturnsNull()
        {
            var schema = BuildMasterDetailSchema();

            var field = schema.FindField("qty", "NoSuchTable");

            Assert.Null(field);
        }

        [Fact]
        [DisplayName("FindField：表存在但欄位不存在時回傳 null")]
        public void FindField_FieldAbsent_ReturnsNull()
        {
            var schema = BuildMasterDetailSchema();

            var field = schema.FindField("no_such_field", "OrderLine");

            Assert.Null(field);
        }

        [Fact]
        [DisplayName("FindField：主檔不存在（ProgId 未對映表）時回傳 null")]
        public void FindField_NoMasterTable_ReturnsNull()
        {
            var schema = new FormSchema();   // ProgId 為空 → MasterTable 為 null

            var field = schema.FindField("sys_id");

            Assert.Null(field);
        }

        [Fact]
        [DisplayName("GetLookupFields：無主檔時回傳空清單")]
        public void GetLookupFields_NoMasterTable_ReturnsEmpty()
        {
            var schema = new FormSchema();   // 無主檔

            var fields = schema.GetLookupFields();

            Assert.Empty(fields);
        }

        [Fact]
        [DisplayName("GetLookupFields：有主檔時回傳 sys_id / sys_name 的預設查詢欄位集")]
        public void GetLookupFields_WithMaster_ReturnsDefaultFields()
        {
            var schema = BuildMasterDetailSchema();

            var fields = schema.GetLookupFields();

            Assert.Contains(fields, f => f.FieldName == "sys_id");
            Assert.Contains(fields, f => f.FieldName == "sys_name");
        }

        [Fact]
        [DisplayName("ToString：回傳「ProgId - DisplayName」格式")]
        public void ToString_ReturnsProgIdDashDisplayName()
        {
            var schema = new FormSchema("Order", "訂單");

            Assert.Equal("Order - 訂單", schema.ToString());
        }
    }
}
