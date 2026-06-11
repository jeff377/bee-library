using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 透過 <see cref="TextEdit"/> 間接驗證 <c>FieldEditorBinder</c> 的事件過濾邏輯：
    /// 不同欄位名稱或不同資料表的 FieldValueChanged 均不應刷新編輯器。
    /// </summary>
    public class FieldEditorBinderTests
    {
        private static FormDataObject BuildMasterOnlyDataObject()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            master.Fields.Add("emp_id", "ID", FieldDbType.String);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static FormDataObject BuildMasterDetailDataObject()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        [Fact]
        [DisplayName("同一資料表中不同欄位變更時編輯器不刷新")]
        public void OnFieldValueChanged_DifferentFieldName_SameTable_DoesNotRefreshEditor()
        {
            var dataObject = BuildMasterOnlyDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");
            dataObject.SetField("emp_name", "Alice");
            Assert.Equal("Alice", editor.Text);

            dataObject.SetField("emp_id", "E001");

            Assert.Equal("Alice", editor.Text);
        }

        [Fact]
        [DisplayName("明細資料表欄位變更時主表編輯器不刷新")]
        public void OnFieldValueChanged_DetailTableChange_DoesNotRefreshMasterEditor()
        {
            var dataObject = BuildMasterDetailDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");
            dataObject.SetField("emp_name", "Alice");
            Assert.Equal("Alice", editor.Text);

            var detailTable = dataObject.DataSet.Tables["EmployeePhone"]!;
            var detailRow = detailTable.NewRow();
            detailRow["phone"] = "123";
            detailTable.Rows.Add(detailRow);

            detailTable.Rows[0]["phone"] = "999";

            Assert.Equal("Alice", editor.Text);
        }

        [Fact]
        [DisplayName("Unbind 後 DataObject 事件不再被處理")]
        public void Unbind_ReleasesFieldValueChangedSubscription()
        {
            var dataObject = BuildMasterOnlyDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");
            dataObject.SetField("emp_name", "Alice");
            editor.Unbind();

            dataObject.SetField("emp_name", "Bob");

            Assert.Equal("Alice", editor.Text);
        }
    }
}
