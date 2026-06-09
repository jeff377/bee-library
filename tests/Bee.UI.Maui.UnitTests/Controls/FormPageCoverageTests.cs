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
    /// 補強 <see cref="FormPage"/> 中尚未覆蓋的刪除操作路徑。
    /// </summary>
    /// <remarks>
    /// 加入 ClientInfo collection 是因為 <see cref="FormPage.InitializeAsync"/>
    /// 讀取 <c>ClientInfo.AccessToken</c> fallback，與
    /// <see cref="FormPageClientInfoTests"/> 可能並行競態，需序列化執行。
    /// </remarks>
    [Collection("ClientInfo")]
    public class FormPageCoverageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
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
        [DisplayName("OnDeleteClickedAsync 成功時 DataObject.MasterRow 應為 null")]
        public async Task OnDeleteClickedAsync_Success_ClearsMasterRow()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var newDataSet = BuildServerDataSet(rowId, "New Employee");
            Guid? deletedRowId = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = BuildListTable(rowId, "Alice") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = newDataSet },
                DeleteHandler = id =>
                {
                    deletedRowId = id;
                    return new DeleteResponse { RowsAffected = 1 };
                },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            var newMethod = typeof(FormPage).GetMethod(
                "OnNewClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(newMethod);
            await (Task)newMethod!.Invoke(page, Array.Empty<object>())!;
            Assert.NotNull(page.DataObject!.MasterRow);

            var deleteMethod = typeof(FormPage).GetMethod(
                "OnDeleteClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(deleteMethod);
            await (Task)deleteMethod!.Invoke(page, Array.Empty<object>())!;

            Assert.Equal(rowId, deletedRowId);
            Assert.Null(page.DataObject.MasterRow);
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 刪除失敗時 ErrorOccurred 應帶出例外訊息")]
        public async Task OnDeleteClickedAsync_DeleteThrows_ReportsError()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var newDataSet = BuildServerDataSet(rowId, "New Employee");
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = BuildListTable(rowId, "Alice") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = newDataSet },
                DeleteHandler = _ => throw new InvalidOperationException("delete rejected"),
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            var newMethod = typeof(FormPage).GetMethod(
                "OnNewClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(newMethod);
            await (Task)newMethod!.Invoke(page, Array.Empty<object>())!;
            Assert.NotNull(page.DataObject!.MasterRow);

            Exception? captured = null;
            page.ErrorOccurred += (_, ex) => captured = ex;

            var deleteMethod = typeof(FormPage).GetMethod(
                "OnDeleteClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(deleteMethod);
            await (Task)deleteMethod!.Invoke(page, Array.Empty<object>())!;

            Assert.NotNull(captured);
            Assert.Equal("delete rejected", captured!.Message);
        }

        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<GetListResponse>? GetListHandler { get; set; }
            public Func<GetNewDataResponse>? GetNewDataHandler { get; set; }
            public Func<Guid, DeleteResponse>? DeleteHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (() => new GetListResponse()))());

            public override Task<GetNewDataResponse> GetNewDataAsync()
                => Task.FromResult((GetNewDataHandler ?? (() => new GetNewDataResponse()))());

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }
    }
}
