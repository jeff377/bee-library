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
    }
}
