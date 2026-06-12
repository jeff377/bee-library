using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.DataObjects
{
    /// <summary>
    /// FormDataObject lookup 寫回測試：ApplyLookupSelection 的 rowid + mapping 寫回、
    /// LookupFieldMappings 優先序、來源欄位缺漏的硬錯誤、ClearLookupSelection 清空。
    /// </summary>
    public class FormDataObjectLookupTests
    {
        private static FormSchema BuildOrderSchema()
        {
            var schema = new FormSchema("Order", "訂單") { CategoryId = "company" };
            var table = schema.Tables!.Add("Order", "訂單");
            table.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            table.Fields!.Add(new FormField(SysFields.Id, "單號", FieldDbType.String));

            var customerField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
            };
            customerField.RelationFieldMappings!.Add(SysFields.Id, "ref_customer_id");
            customerField.RelationFieldMappings!.Add(SysFields.Name, "ref_customer_name");
            table.Fields!.Add(customerField);
            table.Fields!.Add(new FormField("ref_customer_id", "客戶代碼", FieldDbType.String, FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_customer_name", "客戶名稱", FieldDbType.String, FieldType.RelationField));
            return schema;
        }

        private static DataRow BuildSelectedRow(Guid rowId, string id, string name)
        {
            var table = new DataTable("Customer");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add(SysFields.Id, typeof(string));
            table.Columns.Add(SysFields.Name, typeof(string));
            table.Rows.Add(rowId, id, name);
            return table.Rows[0];
        }

        [Fact]
        [DisplayName("ApplyLookupSelection 應寫入 rowid 並依 RelationFieldMappings 寫回 ref 欄位")]
        public void ApplyLookupSelection_WritesRowIdAndMappedFields()
        {
            var dataObject = new FormDataObject(BuildOrderSchema());
            dataObject.InitializeNewMaster();
            var field = dataObject.GetFormField("customer_rowid")!;
            var customerId = Guid.NewGuid();

            dataObject.ApplyLookupSelection(field, BuildSelectedRow(customerId, "C001", "客戶甲"));

            Assert.Equal(customerId.ToString(), dataObject.GetField("customer_rowid"));
            Assert.Equal("C001", dataObject.GetField("ref_customer_id"));
            Assert.Equal("客戶甲", dataObject.GetField("ref_customer_name"));
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("ApplyLookupSelection LookupFieldMappings 應優先於 RelationFieldMappings")]
        public void ApplyLookupSelection_LookupMappingsWin()
        {
            var schema = BuildOrderSchema();
            var schemaField = schema.MasterTable!.Fields!["customer_rowid"];
            schemaField.LookupFieldMappings!.Add(SysFields.Id, "ref_customer_id");

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var field = dataObject.GetFormField("customer_rowid")!;

            dataObject.ApplyLookupSelection(field, BuildSelectedRow(Guid.NewGuid(), "C002", "客戶乙"));

            Assert.Equal("C002", dataObject.GetField("ref_customer_id"));
            // RelationFieldMappings 的 sys_name 映射不應被套用
            Assert.Equal(string.Empty, dataObject.GetField("ref_customer_name"));
        }

        [Fact]
        [DisplayName("ApplyLookupSelection 來源欄位不在選取列應拋 InvalidOperationException")]
        public void ApplyLookupSelection_MissingSourceField_Throws()
        {
            var dataObject = new FormDataObject(BuildOrderSchema());
            dataObject.InitializeNewMaster();
            var field = dataObject.GetFormField("customer_rowid")!;

            // 選取列缺 sys_name（mapping 來源欄位）
            var table = new DataTable("Customer");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add(SysFields.Id, typeof(string));
            table.Rows.Add(Guid.NewGuid(), "C003");

            var ex = Assert.Throws<InvalidOperationException>(
                () => dataObject.ApplyLookupSelection(field, table.Rows[0]));
            Assert.Contains("sys_name", ex.Message);
        }

        [Fact]
        [DisplayName("ApplyLookupSelection 選取列缺 sys_rowid 應拋 InvalidOperationException")]
        public void ApplyLookupSelection_MissingRowId_Throws()
        {
            var dataObject = new FormDataObject(BuildOrderSchema());
            dataObject.InitializeNewMaster();
            var field = dataObject.GetFormField("customer_rowid")!;

            var table = new DataTable("Customer");
            table.Columns.Add(SysFields.Id, typeof(string));
            table.Rows.Add("C004");

            Assert.Throws<InvalidOperationException>(
                () => dataObject.ApplyLookupSelection(field, table.Rows[0]));
        }

        [Fact]
        [DisplayName("ClearLookupSelection 應清空 rowid 與所有映射目的欄位")]
        public void ClearLookupSelection_ResetsRowIdAndMappedFields()
        {
            var dataObject = new FormDataObject(BuildOrderSchema());
            dataObject.InitializeNewMaster();
            var field = dataObject.GetFormField("customer_rowid")!;
            dataObject.ApplyLookupSelection(field, BuildSelectedRow(Guid.NewGuid(), "C001", "客戶甲"));

            dataObject.ClearLookupSelection(field);

            Assert.Equal(Guid.Empty.ToString(), dataObject.GetField("customer_rowid"));
            Assert.Equal(string.Empty, dataObject.GetField("ref_customer_id"));
            Assert.Equal(string.Empty, dataObject.GetField("ref_customer_name"));
        }
    }
}
