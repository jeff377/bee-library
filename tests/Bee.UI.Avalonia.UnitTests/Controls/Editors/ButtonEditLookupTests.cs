using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// ButtonEdit lookup 顯示綁定測試：顯示欄位取值（非 Guid）、lookup 寫回後的
    /// 顯示同步（WatchFieldName）、顯示文字不得寫回 rowid 欄位、lookup 模式下
    /// 文字框恆唯讀。
    /// </summary>
    public class ButtonEditLookupTests
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

        private static (ButtonEdit editor, FormDataObject dataObject, FormField field) BindLookupEditor()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var layout = schema.GetFormLayout();
            var layoutField = layout.Sections![0].Fields!.First(f => f.FieldName == "customer_rowid");

            var editor = new ButtonEdit();
            editor.Bind(dataObject, layoutField);
            return (editor, dataObject, dataObject.GetFormField("customer_rowid")!);
        }

        [Fact]
        [DisplayName("lookup 欄位的 Text 應組合顯示「編號 名稱」而非 Guid")]
        public void RefreshFromSource_ShowsComposedIdAndName()
        {
            var (editor, dataObject, field) = BindLookupEditor();

            dataObject.ApplyLookupSelection(field, BuildSelectedRow(Guid.NewGuid(), "C001", "客戶甲"));

            Assert.Equal("C001 客戶甲", editor.Text);
        }

        [Fact]
        [DisplayName("任一顯示欄位值變更應同步刷新 Text（WatchFieldNames）")]
        public void DisplayFieldChange_RefreshesText()
        {
            var (editor, dataObject, _) = BindLookupEditor();

            // 只有名稱欄有值：編號欄空白會被略過，組合結果只剩名稱。
            dataObject.SetField("ref_customer_name", "客戶乙");

            Assert.Equal("客戶乙", editor.Text);
        }

        [Fact]
        [DisplayName("程式設 Text 不得寫回 rowid 欄位（顯示文字非綁定值）")]
        public void TextAssignment_DoesNotWriteBackToBoundField()
        {
            var (editor, dataObject, field) = BindLookupEditor();
            var customerId = Guid.NewGuid();
            dataObject.ApplyLookupSelection(field, BuildSelectedRow(customerId, "C001", "客戶甲"));

            editor.Text = "hand-typed";

            Assert.Equal(customerId.ToString(), dataObject.GetField("customer_rowid"));
        }

        [Fact]
        [DisplayName("lookup 模式 Edit 狀態下文字框仍唯讀")]
        public void SetControlState_LookupMode_TextStaysReadOnly()
        {
            var (editor, _, _) = BindLookupEditor();

            editor.SetControlState(SingleFormMode.Edit);

            Assert.True(editor.IsReadOnly);
            Assert.True(editor.HasLookup);
        }

        [Fact]
        [DisplayName("row-scoped 綁定（EditForm 模式）lookup 寫回應更新該列並刷新顯示")]
        public void RowScopedBinding_LookupWriteBack_UpdatesRowAndText()
        {
            // 主檔 + 明細：明細列選商品（RowEditPanel / RowEditDialog 的綁定路徑）。
            var schema = new FormSchema("Order", "訂單") { CategoryId = "company" };
            var master = schema.Tables!.Add("Order", "訂單");
            master.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            var detail = schema.Tables!.Add("OrderLine", "訂單明細");
            detail.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            detail.Fields!.Add(new FormField(SysFields.MasterRowId, "主檔識別", FieldDbType.Guid));
            var productField = new FormField("product_rowid", "商品", FieldDbType.Guid)
            {
                RelationProgId = "Product",
            };
            productField.RelationFieldMappings!.Add(SysFields.Name, "ref_product_name");
            detail.Fields!.Add(productField);
            detail.Fields!.Add(new FormField("ref_product_name", "商品名稱", FieldDbType.String, FieldType.RelationField));

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var lineTable = dataObject.DataSet.Tables["OrderLine"]!;
            var line = lineTable.NewRow();
            lineTable.Rows.Add(line);

            var layout = schema.GetFormLayout();
            var column = layout.Details![0].Columns!.First(c => c.FieldName == "product_rowid");
            var editor = new ButtonEdit();
            editor.Bind(dataObject, column, line);

            var productId = Guid.NewGuid();
            var selected = BuildSelectedRow(productId, "P001", "商品甲");
            var field = dataObject.GetFormField("OrderLine", "product_rowid")!;
            dataObject.ApplyLookupSelection(field, selected, line);

            Assert.Equal(productId.ToString(), dataObject.GetField(line, "product_rowid"));
            Assert.Equal("商品甲", dataObject.GetField(line, "ref_product_name"));
            Assert.Equal("商品甲", editor.Text);
        }

        [Fact]
        [DisplayName("非 relation 欄位維持 TextEdit 行為（Edit 可編輯、無 lookup）")]
        public void NonRelationField_KeepsTextEditBehaviour()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();

            var editor = new ButtonEdit();
            editor.Bind(dataObject, SysFields.Id);
            editor.SetControlState(SingleFormMode.Edit);

            Assert.False(editor.HasLookup);
            Assert.False(editor.IsReadOnly);
        }
    }
}
