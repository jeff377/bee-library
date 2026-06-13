using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia.Controls;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// 補強 <see cref="FormView"/> 覆蓋率：Delete 工作流程、工具列狀態、
    /// dirty marker、OnEditClicked no-op 等路徑。
    /// </summary>
    public class FormViewAdditionalTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            schema.ListFields = "sys_id,sys_name";
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("sys_id", "Employee ID", FieldDbType.String);
            master.Fields.Add("sys_name", "Name", FieldDbType.String);
            return schema;
        }

        private static DataSet BuildServerDataSet(Guid rowId, string name)
        {
            var dataSet = new DataSet(TestProgId);
            var master = new DataTable(TestProgId);
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add("sys_name", typeof(string));
            master.Rows.Add(rowId, name);
            dataSet.Tables.Add(master);
            dataSet.AcceptChanges();
            return dataSet;
        }

        private static DataTable BuildEmployeeListTable(Guid rowId, string name)
        {
            var table = new DataTable(TestProgId);
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add(rowId, "E001", name);
            return table;
        }

        private static async Task InvokePrivateAsync(FormView view, string methodName, params object[] args)
        {
            var method = typeof(FormView).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            await (Task)method!.Invoke(view, args)!;
        }

        private static void InvokeEditClicked(FormView view)
        {
            var method = typeof(FormView).GetMethod(
                "OnEditClicked", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(view, null);
        }

        private static Button GetButton(FormView view, string fieldName)
            => (Button)typeof(FormView)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(view)!;

        private static TextBlock GetTextBlock(FormView view, string fieldName)
            => (TextBlock)typeof(FormView)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(view)!;

        private static void InvokeUpdateToolbarState(FormView view)
        {
            var method = typeof(FormView).GetMethod(
                "UpdateToolbarState", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(view, null);
        }

        [Fact]
        [DisplayName("Delete 成功後回到 View 模式並重載清單")]
        public async Task OnDeleteClickedAsync_Success_ReturnsToViewAndReloadsList()
        {
            var rowId = Guid.NewGuid();
            var reloadCount = 0;
            Guid? deletedId = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ =>
                {
                    reloadCount++;
                    return new GetListResponse { Table = BuildEmployeeListTable(rowId, "Alice") };
                },
                GetDataHandler = _ => new GetDataResponse { DataSet = BuildServerDataSet(rowId, "Alice") },
                DeleteHandler = id =>
                {
                    deletedId = id;
                    return new DeleteResponse();
                },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);

            await InvokePrivateAsync(view, "OnDeleteClickedAsync");

            Assert.Equal(rowId, deletedId);
            Assert.Equal(SingleFormMode.View, view.FormMode);
            Assert.Equal(2, reloadCount);
        }

        [Fact]
        [DisplayName("Delete 失敗時 ErrorOccurred 帶出例外訊息")]
        public async Task OnDeleteClickedAsync_Error_FiresErrorOccurred()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "Bob") },
                GetDataHandler = _ => new GetDataResponse { DataSet = BuildServerDataSet(rowId, "Bob") },
                DeleteHandler = _ => throw new InvalidOperationException("delete failed"),
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);
            Exception? captured = null;
            view.ErrorOccurred += (_, ex) => captured = ex;

            await InvokePrivateAsync(view, "OnDeleteClickedAsync");

            Assert.NotNull(captured);
            Assert.Equal("delete failed", captured!.Message);
        }

        [Fact]
        [DisplayName("OnEditClicked 在無 MasterRow 時為 no-op，不變更 FormMode")]
        public async Task OnEditClicked_WithoutMasterRow_IsNoOp()
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse
                {
                    Table = BuildEmployeeListTable(Guid.NewGuid(), "X"),
                },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            Assert.Null(view.DataObject?.MasterRow);

            InvokeEditClicked(view);

            Assert.Equal(SingleFormMode.View, view.FormMode);
        }

        [Fact]
        [DisplayName("View 模式有 MasterRow 時 Edit 與 Delete 按鈕啟用、Save 停用")]
        public async Task UpdateToolbarState_ViewModeWithMasterRow_EnablesEditAndDelete()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "Alice") },
                GetDataHandler = _ => new GetDataResponse { DataSet = BuildServerDataSet(rowId, "Alice") },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);

            Assert.True(GetButton(view, "_editButton").IsEnabled);
            Assert.True(GetButton(view, "_deleteButton").IsEnabled);
            Assert.False(GetButton(view, "_saveButton").IsEnabled);
        }

        [Fact]
        [DisplayName("Edit 模式時 Save 啟用、Edit 與 Delete 停用")]
        public async Task UpdateToolbarState_EditModeWithMasterRow_EnablesSaveOnly()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "Alice") },
                GetDataHandler = _ => new GetDataResponse { DataSet = BuildServerDataSet(rowId, "Alice") },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);
            InvokeEditClicked(view);

            Assert.False(GetButton(view, "_editButton").IsEnabled);
            Assert.False(GetButton(view, "_deleteButton").IsEnabled);
            Assert.True(GetButton(view, "_saveButton").IsEnabled);
        }

        [Fact]
        [DisplayName("資料變更後 UpdateToolbarState 顯示 dirty marker")]
        public async Task DirtyMarker_AfterSetField_IsVisible()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "Alice") },
                GetDataHandler = _ => new GetDataResponse { DataSet = BuildServerDataSet(rowId, "Alice") },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);
            InvokeEditClicked(view);
            Assert.False(GetTextBlock(view, "_dirtyMarker").IsVisible);

            view.DataObject!.SetField("sys_name", "Modified");
            InvokeUpdateToolbarState(view);

            Assert.True(GetTextBlock(view, "_dirtyMarker").IsVisible);
        }

        private sealed class TestFormView : FormView
        {
            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("ClientInfo fallback must not be reached in unit tests.");

            protected override Guid ResolveAccessToken() => Guid.Empty;
        }

        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<string, GetListResponse>? GetListHandler { get; set; }
            public Func<Guid, GetDataResponse>? GetDataHandler { get; set; }
            public Func<GetNewDataResponse>? GetNewDataHandler { get; set; }
            public Func<DataSet, SaveResponse>? SaveHandler { get; set; }
            public Func<Guid, DeleteResponse>? DeleteHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (_ => new GetListResponse()))(selectFields));

            public override Task<GetDataResponse> GetDataAsync(Guid rowId)
                => Task.FromResult((GetDataHandler ?? (_ => new GetDataResponse()))(rowId));

            public override Task<GetNewDataResponse> GetNewDataAsync()
                => Task.FromResult((GetNewDataHandler ?? (() => new GetNewDataResponse()))());

            public override Task<SaveResponse> SaveAsync(DataSet dataSet)
                => Task.FromResult((SaveHandler ?? (_ => new SaveResponse()))(dataSet));

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }
    }
}
