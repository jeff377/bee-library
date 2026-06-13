using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="ButtonEdit"/> 覆蓋率：lookup 無顯示欄位時 RefreshFromSource
    /// 回傳空字串（ApplyMetadata 的 HasLookup+空 displayFields 路徑）、
    /// OpenLookupAsync 在不允許編輯時提早回傳而不呼叫 LookupDialog。
    /// </summary>
    public class ButtonEditCoverageTests
    {
        private static FormDataObject BuildDataObjectWithLookupNoDisplayFields()
        {
            var schema = new FormSchema("Order", "Order");
            var table = schema.Tables!.Add("Order", "Order");
            table.Fields!.Add(new FormField("order_id", "Order ID", FieldDbType.String));
            // lookup 欄無 RelationFieldMappings → GetDisplayFields() 回傳空集合
            table.Fields!.Add(new FormField("vendor_rowid", "Vendor", FieldDbType.Guid)
            {
                RelationProgId = "Vendor",
            });
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static FormDataObject BuildOrderDataObjectWithLookup()
        {
            var schema = new FormSchema("Order", "Order");
            var table = schema.Tables!.Add("Order", "Order");
            table.Fields!.Add(new FormField("order_id", "Order ID", FieldDbType.String));
            var customerField = new FormField("customer_rowid", "Customer", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
            };
            customerField.RelationFieldMappings!.Add("sys_id", "ref_customer_id");
            customerField.RelationFieldMappings!.Add("sys_name", "ref_customer_name");
            table.Fields!.Add(customerField);
            table.Fields!.Add(new FormField("ref_customer_id", "Customer Code", FieldDbType.String,
                Bee.Definition.Database.FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_customer_name", "Customer Name", FieldDbType.String,
                Bee.Definition.Database.FieldType.RelationField));
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static async Task InvokeOnButtonClickAsync(ButtonEdit editor)
        {
            var method = typeof(ButtonEdit).GetMethod(
                "OnButtonClickAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            await (Task)method!.Invoke(editor, null)!;
        }

        [Fact]
        [DisplayName("lookup 欄無 RelationFieldMappings 時 HasLookup 仍為 true 但 Text 顯示空字串")]
        public void RefreshFromSource_LookupWithNoDisplayFields_SetsEmptyText()
        {
            var dataObject = BuildDataObjectWithLookupNoDisplayFields();
            var editor = new ButtonEdit();
            editor.Bind(dataObject, "vendor_rowid");

            Assert.True(editor.HasLookup);
            Assert.Equal(string.Empty, editor.Text);
        }

        [Fact]
        [DisplayName("lookup 欄有值但無 DisplayFields 時刷新也顯示空字串")]
        public void RefreshFromSource_LookupValueSetNoDisplayFields_TextRemainsEmpty()
        {
            var dataObject = BuildDataObjectWithLookupNoDisplayFields();
            var editor = new ButtonEdit();
            editor.Bind(dataObject, "vendor_rowid");

            // 設定 rowid 值，但因無 displayFields → Text 仍為空字串
            dataObject.SetField("vendor_rowid", Guid.NewGuid().ToString());

            Assert.Equal(string.Empty, editor.Text);
        }

        [Fact]
        [DisplayName("OnButtonClickAsync 在 View 模式（不允許編輯）時提早回傳，不呼叫 LookupDialog")]
        public async Task OnButtonClickAsync_LookupInViewMode_ReturnsEarlyWithoutDialog()
        {
            var dataObject = BuildOrderDataObjectWithLookup();
            var editor = new ButtonEdit();
            editor.Bind(dataObject, "customer_rowid");
            editor.SetControlState(SingleFormMode.View);
            Assert.True(editor.HasLookup);

            // 設定 View 模式後 _allowLookupEdit = false → OpenLookupAsync 直接回傳，不進 LookupDialog
            var exception = await Record.ExceptionAsync(() => InvokeOnButtonClickAsync(editor));

            Assert.Null(exception);
        }
    }
}
