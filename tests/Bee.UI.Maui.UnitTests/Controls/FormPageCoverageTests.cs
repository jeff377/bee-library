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
    /// 補強 <see cref="FormPage"/> 的測試覆蓋率：
    /// <c>ComputeSelectFields</c> foreach 路徑、<c>OnDeleteClickedAsync</c>，
    /// 以及 <c>ReloadListAsync</c> 的例外分支。
    /// </summary>
    public class FormPageCoverageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildSchemaWithListFields()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            schema.ListFields = "emp_id,emp_name";
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("emp_id", "ID", FieldDbType.String);
            master.Fields.Add("emp_name", "Name", FieldDbType.String);
            return schema;
        }

        private static DataSet BuildServerDataSet(Guid rowId, string name)
        {
            var dataSet = new DataSet(TestProgId);
            var master = new DataTable(TestProgId);
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add("emp_name", typeof(string));
            master.Rows.Add(rowId, name);
            dataSet.Tables.Add(master);
            dataSet.AcceptChanges();
            return dataSet;
        }

        private static DataTable BuildListTable(Guid rowId)
        {
            var table = new DataTable(TestProgId);
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("emp_id", typeof(string));
            table.Columns.Add("emp_name", typeof(string));
            table.Rows.Add(rowId, "E001", "Alice");
            return table;
        }

        [Fact]
        [DisplayName("schema.ListFields 非空時 GetListAsync 接收到包含 sys_rowid 前綴的 selectFields")]
        public async Task InitializeAsync_WithListFields_PassesSelectFieldsWithSysRowId()
        {
            var schema = BuildSchemaWithListFields();
            string? capturedSelectFields = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = sf =>
                {
                    capturedSelectFields = sf;
                    return new GetListResponse();
                },
            };
            var page = new TestFormPage
            {
                Schema = schema,
                FormConnector = connector,
            };

            await page.InitializeAsync();

            Assert.Equal("sys_rowid,emp_id,emp_name", capturedSelectFields);
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 成功時刪除 master row 並重設資料物件")]
        public async Task OnDeleteClickedAsync_LoadedRow_ResetsDataObject()
        {
            var schema = BuildSchemaWithListFields();
            var rowId = Guid.NewGuid();
            var loadedDataSet = BuildServerDataSet(rowId, "Alice");
            Guid? deletedId = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildListTable(rowId) },
                GetDataHandler = id => new GetDataResponse { DataSet = loadedDataSet },
                DeleteHandler = id =>
                {
                    deletedId = id;
                    return new DeleteResponse { RowsAffected = 1 };
                },
            };
            var page = new TestFormPage
            {
                Schema = schema,
                FormConnector = connector,
            };
            await page.InitializeAsync();

            var loadMethod = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)loadMethod.Invoke(page, new object[] { rowId })!;
            Assert.NotNull(page.DataObject!.MasterRow);

            var deleteMethod = typeof(FormPage).GetMethod(
                "OnDeleteClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)deleteMethod.Invoke(page, Array.Empty<object>())!;

            Assert.Equal(rowId, deletedId);
            Assert.Null(page.DataObject!.MasterRow);
        }

        [Fact]
        [DisplayName("GetListAsync 拋出例外時 ErrorOccurred 帶出例外訊息且初始化仍完成")]
        public async Task ReloadList_WhenGetListThrows_ErrorOccurredFires()
        {
            var schema = BuildSchemaWithListFields();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => throw new InvalidOperationException("list fetch failed"),
            };
            var page = new TestFormPage
            {
                Schema = schema,
                FormConnector = connector,
            };

            Exception? captured = null;
            page.ErrorOccurred += (_, ex) => captured = ex;

            await page.InitializeAsync();

            Assert.NotNull(captured);
            Assert.Equal("list fetch failed", captured!.Message);
            Assert.NotNull(page.DataObject);
        }

        private sealed class TestFormPage : FormPage
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
            public Func<Guid, DeleteResponse>? DeleteHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (_ => new GetListResponse()))(selectFields));

            public override Task<GetDataResponse> GetDataAsync(Guid rowId)
                => Task.FromResult((GetDataHandler ?? (_ => new GetDataResponse()))(rowId));

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }
    }
}
