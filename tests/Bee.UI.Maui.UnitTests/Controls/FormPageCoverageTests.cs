using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Maui.Controls;

namespace Bee.UI.Maui.UnitTests.Controls
{
    /// <summary>
    /// 補強 <see cref="FormPage"/> 尚未覆蓋的路徑：
    /// OnDeleteClickedAsync（成功/失敗）、OnSaveClickedAsync 成功路徑、ComputeSelectFields 去重邏輯。
    /// </summary>
    [Collection("ClientInfo")]
    public class FormPageCoverageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema(string listFields = "")
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            schema.ListFields = listFields;
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
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

        private static DataTable BuildListTable(Guid rowId, string name)
        {
            var table = new DataTable(TestProgId);
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add(rowId, name);
            return table;
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 成功後 DataObject.MasterRow 清空")]
        public async Task OnDeleteClickedAsync_Success_ClearsMasterRow()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var loaded = BuildServerDataSet(rowId, "Alice");
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = BuildListTable(rowId, "Alice") },
                GetDataHandler = _ => new GetDataResponse { DataSet = loaded },
                DeleteHandler = _ => new DeleteResponse { RowsAffected = 1 },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            var loadMethod = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)loadMethod.Invoke(page, new object[] { rowId })!;
            Assert.NotNull(page.DataObject!.MasterRow);

            var deleteMethod = typeof(FormPage).GetMethod(
                "OnDeleteClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)deleteMethod.Invoke(page, Array.Empty<object>())!;

            Assert.Null(page.DataObject.MasterRow);
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 失敗時 ErrorOccurred 攜帶例外訊息")]
        public async Task OnDeleteClickedAsync_Throws_ReportsError()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var loaded = BuildServerDataSet(rowId, "Bob");
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = BuildListTable(rowId, "Bob") },
                GetDataHandler = _ => new GetDataResponse { DataSet = loaded },
                DeleteHandler = _ => throw new InvalidOperationException("delete rejected"),
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            var loadMethod = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)loadMethod.Invoke(page, new object[] { rowId })!;

            Exception? captured = null;
            page.ErrorOccurred += (_, ex) => captured = ex;

            var deleteMethod = typeof(FormPage).GetMethod(
                "OnDeleteClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)deleteMethod.Invoke(page, Array.Empty<object>())!;

            Assert.NotNull(captured);
            Assert.Equal("delete rejected", captured!.Message);
        }

        [Fact]
        [DisplayName("OnSaveClickedAsync 成功後觸發列表重載且 IsDirty 重設為 false")]
        public async Task OnSaveClickedAsync_Success_ReloadsListAndResetsDirty()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var loaded = BuildServerDataSet(rowId, "Charlie");
            var saved = BuildServerDataSet(rowId, "Charlie Updated");
            int listCallCount = 0;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () =>
                {
                    listCallCount++;
                    return new GetListResponse { Table = BuildListTable(rowId, "Charlie") };
                },
                GetDataHandler = _ => new GetDataResponse { DataSet = loaded },
                SaveHandler = _ => new SaveResponse { DataSet = saved },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();
            int countAfterInit = listCallCount;

            var loadRowMethod = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)loadRowMethod.Invoke(page, new object[] { rowId })!;
            page.DataObject!.SetField("sys_name", "Modified");

            var saveMethod = typeof(FormPage).GetMethod(
                "OnSaveClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)saveMethod.Invoke(page, Array.Empty<object>())!;

            Assert.False(page.DataObject.IsDirty);
            Assert.True(listCallCount > countAfterInit);
        }

        [Fact]
        [DisplayName("ComputeSelectFields 在 ListFields 有欄位時先放 sys_rowid 並去除重複欄位")]
        public async Task ComputeSelectFields_WithListFields_PrependsSysRowIdAndDeduplicates()
        {
            // sys_rowid and sys_name both appear multiple times; only unique ones should survive.
            var schema = BuildEmployeeSchema(listFields: "sys_name,sys_rowid,sys_name");
            var connector = new SelectFieldCapturingConnector
            {
                GetListHandler = () => new GetListResponse { Table = BuildListTable(Guid.NewGuid(), "Dave") },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            Assert.NotNull(connector.CapturedSelectFields);
            var parts = connector.CapturedSelectFields!.Split(',');
            Assert.Equal("sys_rowid", parts[0].Trim());
            Assert.Equal(2, parts.Length);
        }

        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<GetListResponse>? GetListHandler { get; set; }
            public Func<Guid, GetDataResponse>? GetDataHandler { get; set; }
            public Func<DataSet, SaveResponse>? SaveHandler { get; set; }
            public Func<Guid, DeleteResponse>? DeleteHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (() => new GetListResponse()))());

            public override Task<GetDataResponse> GetDataAsync(Guid rowId)
                => Task.FromResult((GetDataHandler ?? (_ => new GetDataResponse()))(rowId));

            public override Task<SaveResponse> SaveAsync(DataSet dataSet)
                => Task.FromResult((SaveHandler ?? (_ => new SaveResponse()))(dataSet));

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }

        private sealed class SelectFieldCapturingConnector : FormApiConnector
        {
            public SelectFieldCapturingConnector() : base(Guid.NewGuid(), TestProgId) { }

            public string? CapturedSelectFields { get; private set; }
            public Func<GetListResponse>? GetListHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
            {
                CapturedSelectFields = selectFields;
                return Task.FromResult((GetListHandler ?? (() => new GetListResponse()))());
            }
        }
    }
}
