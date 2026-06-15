using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="GridControl"/> 覆蓋率：RefreshFromDataObject 空 DataObject 路徑、
    /// Unbind 公開方法、RefreshRows、DeleteSelectedRow 無選取路徑、
    /// BuildCellEditor DropDownEdit 無清單項目回退 TextBox。
    /// </summary>
    public class GridControlCoverageTests
    {
        private static FormDataObject BuildDataObjectWithDetail()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);
            detail.Fields.Add("status", "Status", FieldDbType.String);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static void InvokeRefreshFromDataObject(GridControl grid)
        {
            var method = typeof(GridControl).GetMethod(
                "RefreshFromDataObject", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(grid, null);
        }

        [Fact]
        [DisplayName("RefreshFromDataObject 在無 DataObject 時為 no-op，DataTable 維持 null")]
        public void RefreshFromDataObject_NullDataObject_IsNoOp()
        {
            var grid = new GridControl();

            var exception = Record.Exception(() => InvokeRefreshFromDataObject(grid));

            Assert.Null(exception);
            Assert.Null(grid.DataTable);
        }

        [Fact]
        [DisplayName("Unbind 在無綁定狀態下不拋例外")]
        public void Unbind_WhenNotBound_DoesNotThrow()
        {
            var grid = new GridControl();

            var exception = Record.Exception(grid.Unbind);

            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("Unbind 在明細綁定後釋放，後續 DataSetReplaced 不再更新表格")]
        public async Task Unbind_AfterExplicitBind_StopsRefreshOnDataSetReplaced()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var detail = schema.Tables.Add("EmployeePhone", "Phones");
            detail.Fields!.Add("phone", "Phone", FieldDbType.String);

            var refreshed = new DataSet("Employee");
            refreshed.Tables.Add(new DataTable("Employee"));
            var refreshedDetail = new DataTable("EmployeePhone");
            refreshedDetail.Columns.Add("phone", typeof(string));
            refreshedDetail.Rows.Add("new-phone");
            refreshed.Tables.Add(refreshedDetail);

            var connector = new FakeFormApiConnector
            {
                GetNewDataHandler = () => new Bee.Api.Core.Messages.Form.GetNewDataResponse { DataSet = refreshed },
            };
            var dataObject = new FormDataObject(schema, connector);

            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn { FieldName = "phone", Caption = "Phone", Visible = true });

            var grid = new GridControl();
            grid.Bind(dataObject, layout);
            var tableBeforeUnbind = grid.DataTable;

            grid.Unbind();
            await dataObject.NewAsync();

            Assert.Same(tableBeforeUnbind, grid.DataTable);
        }

        [Fact]
        [DisplayName("RefreshRows 在 DataTable 為 null 時清空 ItemsSource 不拋例外")]
        public void RefreshRows_NullDataTable_ClearsItemsSourceSafely()
        {
            var layout = new LayoutGrid("Items", "Items");
            layout.Columns!.Add(new LayoutColumn { FieldName = "name", Caption = "Name", Visible = true });
            var grid = new GridControl();
            grid.Bind(layout, null);

            var exception = Record.Exception(grid.RefreshRows);

            Assert.Null(exception);
            Assert.Null(grid.InnerGrid.ItemsSource);
        }

        [Fact]
        [DisplayName("DeleteSelectedRow 在無選取列時為 no-op，表格列數不變")]
        public void DeleteSelectedRow_NothingSelected_IsNoOp()
        {
            var table = new DataTable("Items");
            table.Columns.Add("name", typeof(string));
            table.Rows.Add("Widget");
            var layout = new LayoutGrid("Items", "Items");
            layout.Columns!.Add(new LayoutColumn { FieldName = "name", Caption = "Name", Visible = true });

            var grid = new GridControl();
            grid.Bind(layout, table);

            var exception = Record.Exception(grid.DeleteSelectedRow);

            Assert.Null(exception);
            Assert.Equal(1, table.Rows.Count);
        }

        [Fact]
        [DisplayName("BuildCellEditor DropDownEdit 欄位無清單項目時回傳 TextBox")]
        public void BuildCellEditor_DropDownEditWithoutListItems_ReturnsTextBox()
        {
            var dataObject = BuildDataObjectWithDetail();
            var layout = new LayoutGrid("EmployeePhone", "Phones");
            layout.Columns!.Add(new LayoutColumn("status", "Status", ControlType.DropDownEdit));

            var grid = new GridControl();
            grid.Bind(dataObject, layout);
            grid.DataTable!.Rows.Add("02-1234", "active");
            var rowView = grid.DataTable.DefaultView[0];

            var method = typeof(GridControl).GetMethod(
                "BuildCellEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var result = method!.Invoke(
                grid,
                new object?[] { rowView, new LayoutColumn("status", "Status", ControlType.DropDownEdit) });

            Assert.IsType<TextBox>(result);
        }

        private sealed class FakeFormApiConnector : Bee.Api.Client.Connectors.FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), "Employee") { }

            public Func<Bee.Api.Core.Messages.Form.GetNewDataResponse>? GetNewDataHandler { get; set; }

            public override Task<Bee.Api.Core.Messages.Form.GetNewDataResponse> GetNewDataAsync()
                => Task.FromResult((GetNewDataHandler ?? (() => new Bee.Api.Core.Messages.Form.GetNewDataResponse()))());
        }
    }
}
