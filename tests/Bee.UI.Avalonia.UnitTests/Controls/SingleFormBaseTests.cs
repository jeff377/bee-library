using System.ComponentModel;
using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// Pipeline tests for <see cref="SingleFormBase"/>: the form owns
    /// <see cref="SingleFormBase.FormMode"/> and broadcasts changes through the
    /// ambient <see cref="FormScope"/>, so every descendant editor and grid switches
    /// state without any per-control <c>SetControlState</c> call. These tests drive
    /// the real broadcast path (property inheritance + class handlers), unlike the
    /// editor tests that call <c>SetControlState</c> directly.
    /// </summary>
    public class SingleFormBaseTests
    {
        private static FormDataObject BuildDataObjectWithDetail()
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

        private static LayoutGrid BuildPhoneLayout()
        {
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            return layout;
        }

        private sealed class TestSingleForm : SingleFormBase
        {
            public int ModeChangedCount { get; private set; }
            public SingleFormMode? LastMode { get; private set; }

            protected override void OnFormModeChanged(SingleFormMode formMode)
            {
                ModeChangedCount++;
                LastMode = formMode;
            }
        }

        private static (TestSingleForm form, Panel panel) BuildForm()
        {
            var panel = new StackPanel();
            var form = new TestSingleForm { Content = panel };
            return (form, panel);
        }

        [Fact]
        [DisplayName("預設 View 並把 ambient scope 釘到 View（覆蓋 Edit 預設）")]
        public void Defaults_PinScopeToView()
        {
            var (form, panel) = BuildForm();
            var probe = new TextBlock();
            panel.Children.Add(probe);

            Assert.Equal(SingleFormMode.View, form.FormMode);
            Assert.Equal(SingleFormMode.View, FormScope.GetFormMode(form));
            // The subtree inherits the form's mode instead of the ambient Edit default.
            Assert.Equal(SingleFormMode.View, FormScope.GetFormMode(probe));
        }

        [Fact]
        [DisplayName("子樹編輯器隨 FormMode 廣播切換唯讀（真實管線，無直接 SetControlState）")]
        public void FormModeBroadcast_TogglesDescendantEditor()
        {
            var dataObject = BuildDataObjectWithDetail();
            var (form, panel) = BuildForm();
            var editor = new TextEdit();
            panel.Children.Add(editor);
            editor.Bind(dataObject, "emp_name");

            // Bound inside a View-mode form: read-only from the start.
            Assert.True(editor.IsReadOnly);

            form.FormMode = SingleFormMode.Edit;
            Assert.False(editor.IsReadOnly);

            form.FormMode = SingleFormMode.View;
            Assert.True(editor.IsReadOnly);
        }

        [Fact]
        [DisplayName("子樹 GridControl 的 AllowEdit 隨 FormMode 廣播切換")]
        public void FormModeBroadcast_TogglesDescendantGrid()
        {
            var dataObject = BuildDataObjectWithDetail();
            var (form, panel) = BuildForm();
            var grid = new GridControl();
            panel.Children.Add(grid);
            grid.Bind(dataObject, BuildPhoneLayout());

            // Bound inside a View-mode form: editing off, toolbar hidden.
            Assert.False(grid.AllowEdit);
            Assert.True(grid.InnerGrid.IsReadOnly);

            form.FormMode = SingleFormMode.Add;
            Assert.True(grid.AllowEdit);
            Assert.False(grid.InnerGrid.IsReadOnly);

            form.FormMode = SingleFormMode.View;
            Assert.False(grid.AllowEdit);
        }

        [Fact]
        [DisplayName("OnFormModeChanged hook 於每次模式變更後被呼叫")]
        public void OnFormModeChanged_InvokedPerChange()
        {
            var (form, _) = BuildForm();

            form.FormMode = SingleFormMode.Add;
            form.FormMode = SingleFormMode.Edit;

            Assert.Equal(2, form.ModeChangedCount);
            Assert.Equal(SingleFormMode.Edit, form.LastMode);
        }

        [Fact]
        [DisplayName("無表單 scope 的獨立編輯器維持 ambient Edit 預設（可編輯）")]
        public void StandaloneEditor_OutsideForm_StaysEditable()
        {
            var dataObject = BuildDataObjectWithDetail();
            var editor = new TextEdit();

            editor.Bind(dataObject, "emp_name");

            Assert.False(editor.IsReadOnly);
        }
    }
}
