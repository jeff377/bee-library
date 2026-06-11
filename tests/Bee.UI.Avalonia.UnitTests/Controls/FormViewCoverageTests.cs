using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// 補強 <see cref="FormView"/> 的測試覆蓋率：
    /// <c>OnDeleteClickedAsync</c>、<c>ReloadListAsync</c> 的例外分支，
    /// 以及 <c>ApplyAccessTokenFallback</c> 寫入 token 的路徑。
    /// </summary>
    public class FormViewCoverageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema()
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

        private static async Task InvokePrivateAsync(FormView view, string methodName, params object[] args)
        {
            var method = typeof(FormView).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            await (Task)method!.Invoke(view, args)!;
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 成功時刪除 master row 並重設資料物件")]
        public async Task OnDeleteClickedAsync_LoadedRow_ResetsDataObject()
        {
            var schema = BuildEmployeeSchema();
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
            var view = new TestFormView
            {
                Schema = schema,
                FormConnector = connector,
            };
            await view.InitializeAsync();

            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);
            Assert.NotNull(view.DataObject!.MasterRow);

            await InvokePrivateAsync(view, "OnDeleteClickedAsync");

            Assert.Equal(rowId, deletedId);
            Assert.Null(view.DataObject!.MasterRow);
        }

        [Fact]
        [DisplayName("GetListAsync 拋出例外時 ErrorOccurred 帶出例外訊息且初始化仍完成")]
        public async Task ReloadList_WhenGetListThrows_ErrorOccurredFires()
        {
            var schema = BuildEmployeeSchema();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => throw new InvalidOperationException("list fetch failed"),
            };
            var view = new TestFormView
            {
                Schema = schema,
                FormConnector = connector,
            };

            Exception? captured = null;
            view.ErrorOccurred += (_, ex) => captured = ex;

            await view.InitializeAsync();

            Assert.NotNull(captured);
            Assert.Equal("list fetch failed", captured!.Message);
            Assert.NotNull(view.DataObject);
        }

        [Fact]
        [DisplayName("AccessToken 為 Guid.Empty 時會 fallback 到 ResolveAccessToken 回傳的 token")]
        public async Task InitializeAsync_EmptyAccessToken_FallsBackToResolvedToken()
        {
            var expectedToken = Guid.NewGuid();
            var schema = BuildEmployeeSchema();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse(),
            };
            var view = new TokenProvidingFormView(expectedToken)
            {
                Schema = schema,
                FormConnector = connector,
            };

            await view.InitializeAsync();

            Assert.Equal(expectedToken, view.AccessToken);
        }

        private sealed class TestFormView : FormView
        {
            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("ClientInfo fallback must not be reached in unit tests.");

            protected override Guid ResolveAccessToken() => Guid.Empty;
        }

        private sealed class TokenProvidingFormView : FormView
        {
            private readonly Guid _token;

            public TokenProvidingFormView(Guid token) { _token = token; }

            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("ClientInfo fallback must not be reached in unit tests.");

            protected override Guid ResolveAccessToken() => _token;
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
