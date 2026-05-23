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
    /// Behaviour tests for <see cref="FormPage"/>. Drives the page through
    /// <see cref="FormPage.InitializeAsync"/> + a <see cref="FakeFormApiConnector"/>
    /// so the unit-test environment never needs a real MAUI handler nor a live
    /// JSON-RPC backend.
    /// </summary>
    public class FormPageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
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
            var table = new DataTable("Employee");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add(rowId, "E001", name);
            return table;
        }

        [Fact]
        [DisplayName("FormPage 為 MAUI ContentView 子類別")]
        public void Type_IsContentViewSubclass()
        {
            Assert.True(typeof(ContentView).IsAssignableFrom(typeof(FormPage)));
        }

        [Theory]
        [InlineData(nameof(FormPage.ProgId), "ProgIdProperty")]
        [InlineData(nameof(FormPage.AccessToken), "AccessTokenProperty")]
        [InlineData(nameof(FormPage.Schema), "SchemaProperty")]
        [InlineData(nameof(FormPage.FormConnector), "FormConnectorProperty")]
        [DisplayName("公開屬性皆有對應的 BindableProperty 註冊")]
        public void PublicProperties_HaveMatchingBindableProperty(string propertyName, string bindablePropertyFieldName)
        {
            var property = typeof(FormPage).GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);

            var bindable = typeof(FormPage).GetField(
                bindablePropertyFieldName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(bindable);
            Assert.Equal(typeof(BindableProperty), bindable!.FieldType);
            Assert.NotNull(bindable.GetValue(null));
        }

        [Fact]
        [DisplayName("缺少 Schema/Connector 時 InitializeAsync 為 no-op")]
        public async Task InitializeAsync_MissingInputs_IsNoOp()
        {
            var page = new FormPage();

            await page.InitializeAsync();

            Assert.Null(page.DataObject);
        }

        [Fact]
        [DisplayName("Schema + FormConnector 都設好後 InitializeAsync 會建立 FormDataObject 並載入列表")]
        public async Task InitializeAsync_WithInputs_BuildsDataObjectAndLoadsList()
        {
            var schema = BuildEmployeeSchema();
            var listTable = BuildEmployeeListTable(Guid.NewGuid(), "Alice");
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = listTable },
            };
            var page = new FormPage
            {
                Schema = schema,
                FormConnector = connector,
            };

            await page.InitializeAsync();

            Assert.NotNull(page.DataObject);
            Assert.Same(schema, page.DataObject!.GetType()
                .GetField("_schema", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(page.DataObject));
        }

        [Fact]
        [DisplayName("InitializeAsync 不會在 Schema 或 Connector 任一缺失時觸發 FormDataObject 建構")]
        public async Task InitializeAsync_OnlySchema_DoesNotBuildDataObject()
        {
            var page = new FormPage { Schema = BuildEmployeeSchema() };

            await page.InitializeAsync();

            Assert.Null(page.DataObject);
        }

        [Fact]
        [DisplayName("OnRowSelectedAsync 會委派到 FormDataObject.LoadAsync")]
        public async Task OnRowSelected_DelegatesToLoadAsync()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var loadedDataSet = BuildServerDataSet(rowId, "Loaded Bob");
            Guid? requestedRowId = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = BuildEmployeeListTable(rowId, "Bob") },
                GetDataHandler = id =>
                {
                    requestedRowId = id;
                    return new GetDataResponse { DataSet = loadedDataSet };
                },
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            var method = typeof(FormPage).GetMethod(
                "OnRowSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var task = (Task)method!.Invoke(page, new object[] { rowId })!;
            await task;

            Assert.Equal(rowId, requestedRowId);
            Assert.NotNull(page.DataObject!.MasterRow);
            Assert.Equal("Loaded Bob", page.DataObject!.GetField("sys_name"));
        }

        [Fact]
        [DisplayName("Save / Delete 失敗時 ErrorOccurred 會帶出例外訊息")]
        public async Task ErrorOccurred_FiresWhenSaveThrows()
        {
            var schema = BuildEmployeeSchema();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = BuildEmployeeListTable(Guid.NewGuid(), "X") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = BuildServerDataSet(Guid.NewGuid(), "Pending") },
                SaveHandler = _ => throw new InvalidOperationException("backend rejected"),
            };
            var page = new FormPage { Schema = schema, FormConnector = connector };
            await page.InitializeAsync();

            Exception? captured = null;
            page.ErrorOccurred += (_, ex) => captured = ex;

            var newMethod = typeof(FormPage).GetMethod(
                "OnNewClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)newMethod!.Invoke(page, Array.Empty<object>())!;
            Assert.NotNull(page.DataObject!.MasterRow);

            var saveMethod = typeof(FormPage).GetMethod(
                "OnSaveClickedAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            await (Task)saveMethod!.Invoke(page, Array.Empty<object>())!;

            Assert.NotNull(captured);
            Assert.Equal("backend rejected", captured!.Message);
        }

        /// <summary>
        /// Same test double the FormDataObject tests use; overrides every virtual
        /// CRUD method on <see cref="FormApiConnector"/> so the base
        /// <c>LocalApiProvider</c> is never reached.
        /// </summary>
        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<GetListResponse>? GetListHandler { get; set; }
            public Func<Guid, GetDataResponse>? GetDataHandler { get; set; }
            public Func<GetNewDataResponse>? GetNewDataHandler { get; set; }
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

            public override Task<GetNewDataResponse> GetNewDataAsync()
                => Task.FromResult((GetNewDataHandler ?? (() => new GetNewDataResponse()))());

            public override Task<SaveResponse> SaveAsync(DataSet dataSet)
                => Task.FromResult((SaveHandler ?? (_ => new SaveResponse()))(dataSet));

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }
    }
}
