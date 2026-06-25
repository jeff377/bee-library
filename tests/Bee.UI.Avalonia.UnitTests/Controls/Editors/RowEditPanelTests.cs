using System.ComponentModel;
using System.Data;
using Avalonia.Input;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Behaviour checks for <see cref="RowEditPanel"/>: buffered edit session
    /// lifecycle (bind / commit / cancel / rebind) through the
    /// <see cref="FormDataObject"/> row edit protocol.
    /// </summary>
    public class RowEditPanelTests
    {
        private static FormDataObject BuildDataObject()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);
            detail.Fields.Add("is_primary", "Primary", FieldDbType.Boolean);

            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var table = dataObject.DataSet.Tables["EmployeePhone"]!;
            table.Rows.Add("02-1234-5678", false);
            table.Rows.Add("0912-345-678", true);
            table.AcceptChanges();
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static LayoutGrid BuildLayout()
        {
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn("phone", "Phone", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("is_primary", "Primary", ControlType.CheckEdit));
            layout.Columns.Add(new LayoutColumn("hidden", "Hidden", ControlType.TextEdit) { Visible = false });
            return layout;
        }

        private static T FindEditor<T>(RowEditPanel panel)
            where T : global::Avalonia.Controls.Control
        {
            var host = Assert.IsType<global::Avalonia.Controls.StackPanel>(panel.Content);
            var grid = Assert.IsType<global::Avalonia.Controls.Grid>(host.Children[0]);
            return grid.Children
                .OfType<global::Avalonia.Controls.StackPanel>()
                .Select(cell => cell.Children[1])
                .OfType<T>()
                .First();
        }

        [Fact]
        [DisplayName("Bind 啟動編輯 session 並依可見欄位產生編輯器")]
        public void Bind_BuildsEditorsAndStartsSession()
        {
            var dataObject = BuildDataObject();
            var row = dataObject.DataSet.Tables["EmployeePhone"]!.Rows[0];
            var panel = new RowEditPanel();

            panel.Bind(dataObject, BuildLayout(), row);

            Assert.Same(row, panel.Row);
            Assert.True(row.HasVersion(DataRowVersion.Proposed));
            var textEditor = FindEditor<TextEdit>(panel);
            Assert.Equal("02-1234-5678", textEditor.Text);
            var checkEditor = FindEditor<CheckEdit>(panel);
            Assert.False(checkEditor.IsChecked);
            // The invisible column produces no editor: one text + one check only.
            var host = (global::Avalonia.Controls.StackPanel)panel.Content!;
            var grid = (global::Avalonia.Controls.Grid)host.Children[0];
            Assert.Equal(2, grid.Children.Count);
        }

        [Fact]
        [DisplayName("Commit 落實編輯、補發事件並標 dirty")]
        public void Commit_WritesThroughAndPublishes()
        {
            var dataObject = BuildDataObject();
            var row = dataObject.DataSet.Tables["EmployeePhone"]!.Rows[0];
            var panel = new RowEditPanel();
            panel.Bind(dataObject, BuildLayout(), row);

            var raised = new List<FieldValueChangedEventArgs>();
            dataObject.FieldValueChanged += (_, e) => raised.Add(e);
            var committedRaised = 0;
            panel.EditCommitted += (_, _) => committedRaised++;

            var editor = FindEditor<TextEdit>(panel);
            editor.Text = "07-999-8888";
            // Commit-on-leave: the value writes to the buffered row when the field commits
            // (Enter here; clicking OK blurs the field in the real UI), still suppressed by
            // the edit session.
            editor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            Assert.Empty(raised);   // buffered: nothing publishes during the session

            panel.Commit();

            Assert.Equal("07-999-8888", row["phone"]);
            Assert.Equal(1, committedRaised);
            var args = Assert.Single(raised);
            Assert.Equal("phone", args.FieldName, ignoreCase: true);
            Assert.True(dataObject.IsDirty);
            Assert.Null(panel.Row);
        }

        [Fact]
        [DisplayName("Cancel 完整還原、零事件、不弄髒")]
        public void Cancel_RestoresSilently()
        {
            var dataObject = BuildDataObject();
            var row = dataObject.DataSet.Tables["EmployeePhone"]!.Rows[0];
            var panel = new RowEditPanel();
            panel.Bind(dataObject, BuildLayout(), row);

            var raisedCount = 0;
            dataObject.FieldValueChanged += (_, _) => raisedCount++;
            var cancelledRaised = 0;
            panel.EditCancelled += (_, _) => cancelledRaised++;

            var editor = FindEditor<TextEdit>(panel);
            editor.Text = "07-999-8888";
            editor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
            panel.Cancel();

            Assert.Equal("02-1234-5678", row["phone"]);
            Assert.Equal(0, raisedCount);
            Assert.Equal(1, cancelledRaised);
            Assert.False(dataObject.IsDirty);
        }

        [Theory]
        [InlineData(false, 2)]  // wide screen → two columns
        [InlineData(true, 1)]   // compact screen → single column
        [DisplayName("Compact 旗標決定編輯表單欄數")]
        public void Compact_DrivesColumnCount(bool compact, int expectedColumns)
        {
            var dataObject = BuildDataObject();
            var row = dataObject.DataSet.Tables["EmployeePhone"]!.Rows[0];
            var panel = new RowEditPanel { Compact = compact };

            panel.Bind(dataObject, BuildLayout(), row);

            var host = Assert.IsType<global::Avalonia.Controls.StackPanel>(panel.Content);
            var grid = Assert.IsType<global::Avalonia.Controls.Grid>(host.Children[0]);
            Assert.Equal(expectedColumns, grid.ColumnDefinitions.Count);
        }

        [Theory]
        [InlineData(400.0, true)]   // phone-sized → compact
        [InlineData(900.0, false)]  // desktop → not compact
        [InlineData(600.0, false)]  // exactly threshold → not compact
        [InlineData(0.0, false)]    // unmeasured → not compact
        [DisplayName("IsCompactWidth 依螢幕寬度判定 compact")]
        public void IsCompactWidth_ByScreenWidth(double width, bool expected)
        {
            Assert.Equal(expected, RowEditPanel.IsCompactWidth(width));
        }

        [Fact]
        [DisplayName("重複 Bind 取消前一筆 session")]
        public void Rebind_CancelsPreviousSession()
        {
            var dataObject = BuildDataObject();
            var table = dataObject.DataSet.Tables["EmployeePhone"]!;
            var panel = new RowEditPanel();

            panel.Bind(dataObject, BuildLayout(), table.Rows[0]);
            FindEditor<TextEdit>(panel).Text = "07-999-8888";

            panel.Bind(dataObject, BuildLayout(), table.Rows[1]);

            Assert.Equal("02-1234-5678", table.Rows[0]["phone"]);
            Assert.False(table.Rows[0].HasVersion(DataRowVersion.Proposed));
            Assert.Same(table.Rows[1], panel.Row);
        }
    }
}
