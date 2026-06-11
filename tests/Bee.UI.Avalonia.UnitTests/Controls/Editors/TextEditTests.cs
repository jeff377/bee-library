using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Behaviour checks for <see cref="TextEdit"/> (and the <see cref="FieldEditorBinder"/>
    /// plumbing it shares with the other editors): explicit bind, metadata application,
    /// write-back, event-driven refresh and form-mode state.
    /// </summary>
    public class TextEditTests
    {
        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var empId = master.Fields.Add("emp_id", "ID", FieldDbType.String);
            empId.MaxLength = 10;
            return schema;
        }

        private static FormDataObject BuildDataObject()
        {
            var dataObject = new FormDataObject(BuildSchema());
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        [Fact]
        [DisplayName("Bind 後載入欄位初值")]
        public void Bind_ExistingValue_LoadsIntoText()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("emp_name", "Alice");

            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");

            Assert.Equal("Alice", editor.Text);
        }

        [Fact]
        [DisplayName("輸入文字寫回 FormDataObject")]
        public void TextChanged_AfterBind_WritesBack()
        {
            var dataObject = BuildDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");

            editor.Text = "Bob";

            Assert.Equal("Bob", dataObject.GetField("emp_name"));
            Assert.True(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("Bind 後套用 FormField.MaxLength")]
        public void Bind_FieldWithMaxLength_AppliesMaxLength()
        {
            var dataObject = BuildDataObject();
            var editor = new TextEdit();

            editor.Bind(dataObject, "emp_id");

            Assert.Equal(10, editor.MaxLength);
        }

        [Fact]
        [DisplayName("Bind 帶 LayoutField.ReadOnly 時編輯器唯讀")]
        public void Bind_ReadOnlyLayoutField_SetsIsReadOnly()
        {
            var dataObject = BuildDataObject();
            var field = new LayoutField { FieldName = "emp_name", ReadOnly = true };
            var editor = new TextEdit();

            editor.Bind(dataObject, field);

            Assert.True(editor.IsReadOnly);
        }

        [Fact]
        [DisplayName("他方 SetField 同欄位時編輯器自動刷新")]
        public void FieldValueChanged_OtherWriter_RefreshesEditor()
        {
            var dataObject = BuildDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");

            dataObject.SetField("emp_name", "Carol");

            Assert.Equal("Carol", editor.Text);
        }

        [Fact]
        [DisplayName("DataSetReplaced 後編輯器重拉值")]
        public void DataSetReplaced_AfterBind_RefreshesEditor()
        {
            var dataObject = BuildDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");
            editor.Text = "Bob";

            dataObject.InitializeNewMaster();

            Assert.Equal(string.Empty, editor.Text);
        }

        [Fact]
        [DisplayName("編輯器刷新不回寫、不弄髒資料（echo 防護）")]
        public void Refresh_FromSource_DoesNotDirtyDataObject()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("emp_name", "Alice");
            var editor = new TextEdit();

            editor.Bind(dataObject, "emp_name");
            dataObject.InitializeNewMaster();

            Assert.False(dataObject.IsDirty);
        }

        [Fact]
        [DisplayName("SetControlState View 模式唯讀，Edit 模式可編輯")]
        public void SetControlState_ViewMode_TogglesReadOnly()
        {
            var dataObject = BuildDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");

            editor.SetControlState(SingleFormMode.View);
            Assert.True(editor.IsReadOnly);

            editor.SetControlState(SingleFormMode.Edit);
            Assert.False(editor.IsReadOnly);
        }

        [Fact]
        [DisplayName("Unbind 後輸入不再寫回")]
        public void Unbind_AfterBind_StopsWriteBack()
        {
            var dataObject = BuildDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");

            editor.Unbind();
            editor.Text = "Bob";

            Assert.Equal(string.Empty, dataObject.GetField("emp_name"));
        }

        [Fact]
        [DisplayName("列綁定：載入明細列值、寫回該列、套用明細表 metadata")]
        public void BindRow_DetailRow_LoadsWritesAndAppliesMetadata()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            var phone = detail.Fields!.Add("phone", "Phone", FieldDbType.String);
            phone.MaxLength = 15;

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var table = dataObject.DataSet.Tables["EmployeePhone"]!;
            table.Rows.Add("02-1234-5678");
            var row = table.Rows[0];

            var editor = new TextEdit();
            editor.Bind(dataObject, new LayoutColumn("phone", "Phone", ControlType.TextEdit), row);

            Assert.Equal("02-1234-5678", editor.Text);
            Assert.Equal(15, editor.MaxLength);

            editor.Text = "0912-345-678";
            Assert.Equal("0912-345-678", row["phone"]);
        }

        [Fact]
        [DisplayName("列綁定：他列變更不刷新、本列他方變更會刷新")]
        public void BindRow_EventFiltering_MatchesTargetRowOnly()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var table = dataObject.DataSet.Tables["EmployeePhone"]!;
            table.Rows.Add("row0");
            table.Rows.Add("row1");

            var editor = new TextEdit();
            editor.Bind(dataObject, new LayoutColumn("phone", "Phone", ControlType.TextEdit), table.Rows[0]);

            dataObject.SetField(table.Rows[1], "phone", "other-row");
            Assert.Equal("row0", editor.Text);

            dataObject.SetField(table.Rows[0], "phone", "target-row");
            Assert.Equal("target-row", editor.Text);
        }

        [Fact]
        [DisplayName("MemoEdit 預設多行設定")]
        public void MemoEdit_Defaults_AreMultiLine()
        {
            var editor = new MemoEdit();

            Assert.True(editor.AcceptsReturn);
            Assert.Equal(global::Avalonia.Media.TextWrapping.Wrap, editor.TextWrapping);
            Assert.Equal(60, editor.MinHeight);
        }

        [Fact]
        [DisplayName("ButtonEdit 內嵌按鈕並轉發 ButtonClick")]
        public void ButtonEdit_EmbeddedButton_RaisesButtonClick()
        {
            var editor = new ButtonEdit();
            var button = Assert.IsType<global::Avalonia.Controls.Button>(editor.InnerRightContent);

            var raised = 0;
            editor.ButtonClick += (_, _) => raised++;
            button.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(
                global::Avalonia.Controls.Button.ClickEvent));

            Assert.Equal(1, raised);
        }

        [Fact]
        [DisplayName("ButtonEdit View 模式停用內嵌按鈕，Edit 模式恢復")]
        public void ButtonEdit_SetControlState_TogglesButtonEnabled()
        {
            var dataObject = BuildDataObject();
            var editor = new ButtonEdit();
            editor.Bind(dataObject, "emp_name");
            var button = Assert.IsType<global::Avalonia.Controls.Button>(editor.InnerRightContent);

            editor.SetControlState(SingleFormMode.View);
            Assert.False(button.IsEnabled);

            editor.SetControlState(SingleFormMode.Edit);
            Assert.True(button.IsEnabled);
        }

        [Fact]
        [DisplayName("ButtonEdit 綁定 ReadOnly LayoutField 時停用內嵌按鈕")]
        public void ButtonEdit_BindReadOnlyLayoutField_DisablesButton()
        {
            var dataObject = BuildDataObject();
            var field = new LayoutField { FieldName = "emp_name", ReadOnly = true };
            var editor = new ButtonEdit();

            editor.Bind(dataObject, field);

            var button = Assert.IsType<global::Avalonia.Controls.Button>(editor.InnerRightContent);
            Assert.False(button.IsEnabled);
        }
    }
}
