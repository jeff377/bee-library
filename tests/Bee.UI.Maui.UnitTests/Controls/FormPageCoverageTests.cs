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
    /// Additional coverage tests for <see cref="FormPage"/>: targets paths not
    /// exercised by <see cref="FormPageTests"/> or <see cref="FormPageClientInfoTests"/>
    /// — specifically <c>ComputeSelectFields</c>, the already-initialised guard,
    /// <c>OnDeleteClickedAsync</c>, and the busy-reentrance guard in
    /// <c>RunGuardedAsync</c>.
    /// </summary>
    /// <remarks>
    /// Joins the <c>ClientInfo</c> xUnit collection because <see cref="FormPage.InitializeAsync"/>
    /// reads <c>ClientInfo.AccessToken</c> when the page's own token is
    /// <see cref="Guid.Empty"/>, and <see cref="FormPageClientInfoTests"/> running
    /// in parallel mutates that static. The shared collection serialises all three
    /// classes against the same process-wide state.
    /// </remarks>
    [Collection("ClientInfo")]
    public class FormPageCoverageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema(string? listFields = null)
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            if (listFields is not null)
                schema.ListFields = listFields;
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

        [Fact]
        [DisplayName("ListFields 有值時 InitializeAsync 傳給 GetListAsync 的 selectFields 以 sys_rowid 開頭並包含 schema 欄位")]
        public async Task ComputeSelectFields_WithListFields_PrependsSysRowIdAndIncludesSchemaFields()
        {
            var schema = BuildEmployeeSchema(listFields: "sys_id,sys_name");
            var connector = new CapturingFormApiConnector
            {
                ListTable = BuildEmployeeListTable(Guid.NewGuid(), "Alice"),
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };

            await page.InitializeAsync();

            Assert.NotNull(connector.CapturedSelectFields);
            Assert.StartsWith($"{SysFields.RowId},", connector.CapturedSelectFields, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sys_id", connector.CapturedSelectFields, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sys_name", connector.CapturedSelectFields, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [DisplayName("InitializeAsync 第二次呼叫為 no-op，不再觸發後端 GetList")]
        public async Task InitializeAsync_CalledTwice_OnlyInitializesOnce()
        {
            var schema = BuildEmployeeSchema();
            var callCount = 0;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () =>
                {
                    callCount++;
                    return new GetListResponse { Table = BuildEmployeeListTable(Guid.NewGuid(), "Alice") };
                },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };

            await page.InitializeAsync();
            await page.InitializeAsync();

            Assert.Equal(1, callCount);
        }

        [Fact]
        [DisplayName("Delete 按鈕成功刪除後應呼叫 DeleteAsync 並重新載入列表")]
        public async Task OnDeleteClickedAsync_WithLoadedRecord_CallsDeleteAndReloadsList()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            Guid? deletedId = null;
            var reloadCount = 0;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () =>
                {
                    reloadCount++;
                    return new GetListResponse { Table = BuildEmployeeListTable(rowId, "Alice") };
                },
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Alice") },
                DeleteHandler = id =>
                {
                    deletedId = id;
                    return new DeleteResponse { RowsAffected = 1 };
                },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            var rowSelectMethod = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(rowSelectMethod);
            await (Task)rowSelectMethod!.Invoke(page, new object[] { rowId })!;

            var deleteMethod = typeof(FormPage).GetMethod(
                "OnDeleteClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(deleteMethod);
            await (Task)deleteMethod!.Invoke(page, Array.Empty<object>())!;

            Assert.Equal(rowId, deletedId);
            Assert.Equal(2, reloadCount);
        }

        [Fact]
        [DisplayName("RunGuardedAsync 在忙碌狀態時跳過第二次呼叫，不重複執行 GetNewData")]
        public async Task RunGuardedAsync_WhenBusy_SkipsSecondCall()
        {
            var schema = BuildEmployeeSchema();
            var blockTcs = new TaskCompletionSource<GetNewDataResponse>();
            var blockingConnector = new BlockingFormApiConnector(blockTcs)
            {
                ListTable = BuildEmployeeListTable(Guid.NewGuid(), "X"),
            };
            var page = new FormPage { Schema = schema, FormConnector = blockingConnector };
            await page.InitializeAsync();

            var newMethod = typeof(FormPage).GetMethod(
                "OnNewClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(newMethod);

            // 第一次呼叫：_dataObject.NewAsync 阻塞，_isBusy 已設為 true
            var firstTask = (Task)newMethod!.Invoke(page, Array.Empty<object>())!;

            // 第二次呼叫：_isBusy == true，應立即 return（no-op）
            var secondTask = (Task)newMethod.Invoke(page, Array.Empty<object>())!;
            await secondTask;

            // 解除第一次呼叫的阻塞
            blockTcs.SetResult(new GetNewDataResponse { DataSet = BuildServerDataSet(Guid.NewGuid(), "New") });
            await firstTask;

            Assert.Equal(1, blockingConnector.NewCallCount);
        }

        private sealed class CapturingFormApiConnector : FormApiConnector
        {
            public string? CapturedSelectFields { get; private set; }
            public DataTable? ListTable { get; set; }

            public CapturingFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
            {
                CapturedSelectFields = selectFields;
                return Task.FromResult(new GetListResponse { Table = ListTable });
            }
        }

        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<GetListResponse>? GetListHandler { get; set; }
            public Func<Guid, GetDataResponse>? GetDataHandler { get; set; }
            public Func<Guid, DeleteResponse>? DeleteHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (() => new GetListResponse()))());

            public override Task<GetDataResponse> GetDataAsync(Guid rowId)
                => Task.FromResult((GetDataHandler ?? (_ => new GetDataResponse()))(rowId));

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }

        private sealed class BlockingFormApiConnector : FormApiConnector
        {
            private readonly TaskCompletionSource<GetNewDataResponse> _newTcs;

            public int NewCallCount { get; private set; }
            public DataTable? ListTable { get; set; }

            public BlockingFormApiConnector(TaskCompletionSource<GetNewDataResponse> newTcs)
                : base(Guid.NewGuid(), TestProgId)
            {
                _newTcs = newTcs;
            }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult(new GetListResponse { Table = ListTable });

            public override Task<GetNewDataResponse> GetNewDataAsync()
            {
                NewCallCount++;
                return _newTcs.Task;
            }
        }
    }
}
