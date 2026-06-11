using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="GridControl"/> 及 <c>GridControlBinder</c> 的覆蓋率：
    /// AddRow / DeleteSelectedRow 的 no-op 情境、Unbind 取消訂閱、列表模式接替明細模式。
    /// </summary>
    public class GridControlExtendedTests
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

        private static LayoutGrid BuildDetailLayout()
        {
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });
            return layout;
        }

        private static LayoutGrid BuildListLayout()
        {
            var layout = new LayoutGrid("Employee", "Employees");
            layout.Columns!.Add(new LayoutColumn { FieldName = "sys_id", Caption = "ID", Visible = true });
            return layout;
        }

        private static DataTable BuildListRows()
        {
            var table = new DataTable("Employee");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Rows.Add(Guid.NewGuid(), "E001");
            return table;
        }

        [Fact]
        [DisplayName("AddRow DataTable 為 null 時為 no-op")]
        public void AddRow_NullDataTable_IsNoOp()
        {
            var grid = new GridControl();

            var exception = Record.Exception(grid.AddRow);

            Assert.Null(exception);
            Assert.Null(grid.DataTable);
        }

        [Fact]
        [DisplayName("DeleteSelectedRow 無選取時為 no-op")]
        public void DeleteSelectedRow_NothingSelected_IsNoOp()
        {
            var grid = new GridControl();
            var layout = BuildListLayout();
            grid.Bind(layout, BuildListRows());

            var exception = Record.Exception(grid.DeleteSelectedRow);

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("Unbind 在無綁定時不拋例外")]
        public void Unbind_WithNoBinding_IsNoOp()
        {
            var grid = new GridControl();

            var exception = Record.Exception(grid.Unbind);

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("Unbind 取消明細 DataObject 的 DataSetReplaced 訂閱")]
        public void Unbind_AfterDetailBind_StopsDataObjectUpdates()
        {
            var dataObject = BuildDataObjectWithDetail();
            var grid = new GridControl();
            grid.Bind(dataObject, BuildDetailLayout());
            var tableBeforeUnbind = grid.DataTable;

            grid.Unbind();
            dataObject.InitializeNewMaster();

            Assert.Same(tableBeforeUnbind, grid.DataTable);
        }

        [Fact]
        [DisplayName("列表模式 Bind 在明細模式之後取消 DataObject 的 DataSetReplaced 訂閱")]
        public void Bind_ListModeAfterDetailBind_UnsubscribesDataObjectEvents()
        {
            var dataObject = BuildDataObjectWithDetail();
            var listRows = BuildListRows();
            var grid = new GridControl();
            grid.Bind(dataObject, BuildDetailLayout());

            grid.Bind(BuildListLayout(), listRows);
            dataObject.InitializeNewMaster();

            Assert.Same(listRows, grid.DataTable);
        }
    }
}
