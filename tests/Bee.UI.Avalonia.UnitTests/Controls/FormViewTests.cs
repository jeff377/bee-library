using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// Behaviour tests for <see cref="FormView"/>. Drives the view through
    /// <see cref="FormView.InitializeAsync"/> + a <see cref="FakeFormApiConnector"/>
    /// so the unit-test environment never needs an Avalonia visual tree nor a live
    /// JSON-RPC backend. A <see cref="TestFormView"/> subclass overrides the
    /// <c>Resolve*</c> hooks so the static <c>ClientInfo</c> is never touched.
    /// </summary>
    public class FormViewTests
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

        [Fact]
        [DisplayName("FormView 為 SingleFormBase 資料表單子類別")]
        public void Type_IsSingleFormBaseSubclass()
        {
            Assert.True(typeof(UserControl).IsAssignableFrom(typeof(FormView)));
            Assert.True(typeof(SingleFormBase).IsAssignableFrom(typeof(FormView)));
        }

        [Theory]
        [InlineData(nameof(FormView.ProgId), "ProgIdProperty")]
        [InlineData(nameof(FormView.AccessToken), "AccessTokenProperty")]
        [InlineData(nameof(FormView.Schema), "SchemaProperty")]
        [InlineData(nameof(FormView.FormConnector), "FormConnectorProperty")]
        [DisplayName("公開屬性皆有對應的 StyledProperty 註冊")]
        public void PublicProperties_HaveMatchingStyledProperty(string propertyName, string styledPropertyFieldName)
        {
            var property = typeof(FormView).GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);

            var styled = typeof(FormView).GetField(
                styledPropertyFieldName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(styled);
            Assert.True(typeof(AvaloniaProperty).IsAssignableFrom(styled!.FieldType));
            Assert.NotNull(styled.GetValue(null));
        }

        [Fact]
        [DisplayName("缺少 Schema/Connector 時 InitializeAsync 為 no-op")]
        public async Task InitializeAsync_MissingInputs_IsNoOp()
        {
            var view = new TestFormView();

            await view.InitializeAsync();

            Assert.Null(view.DataObject);
        }

        [Fact]
        [DisplayName("只設 Schema 時 InitializeAsync 不建立 FormDataObject")]
        public async Task InitializeAsync_OnlySchema_DoesNotBuildDataObject()
        {
            var view = new TestFormView { Schema = BuildEmployeeSchema() };

            await view.InitializeAsync();

            Assert.Null(view.DataObject);
        }

        [Fact]
        [DisplayName("Schema + FormConnector 都設好後 InitializeAsync 會建立 FormDataObject 並載入列表")]
        public async Task InitializeAsync_WithInputs_BuildsDataObjectAndLoadsList()
        {
            var schema = BuildEmployeeSchema();
            string? capturedSelectFields = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = selectFields =>
                {
                    capturedSelectFields = selectFields;
                    return new GetListResponse { Table = BuildEmployeeListTable(Guid.NewGuid(), "Alice") };
                },
            };
            var view = new TestFormView
            {
                Schema = schema,
                FormConnector = connector,
            };

            await view.InitializeAsync();

            Assert.NotNull(view.DataObject);
            // ComputeSelectFields must prepend sys_rowid ahead of FormSchema.ListFields
            // so the wire response carries the identifier row selection needs.
            Assert.Equal("sys_rowid,sys_id,sys_name", capturedSelectFields);
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
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "Bob") },
                GetDataHandler = id =>
                {
                    requestedRowId = id;
                    return new GetDataResponse { DataSet = loadedDataSet };
                },
            };
            var view = new TestFormView { Schema = schema, FormConnector = connector };
            await view.InitializeAsync();

            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);

            Assert.Equal(rowId, requestedRowId);
            Assert.NotNull(view.DataObject!.MasterRow);
            Assert.Equal("Loaded Bob", view.DataObject!.GetField("sys_name"));
        }

        [Fact]
        [DisplayName("Save 失敗時 ErrorOccurred 會帶出例外訊息")]
        public async Task ErrorOccurred_FiresWhenSaveThrows()
        {
            var schema = BuildEmployeeSchema();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(Guid.NewGuid(), "X") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = BuildServerDataSet(Guid.NewGuid(), "Pending") },
                SaveHandler = _ => throw new InvalidOperationException("backend rejected"),
            };
            var view = new TestFormView { Schema = schema, FormConnector = connector };
            await view.InitializeAsync();

            Exception? captured = null;
            view.ErrorOccurred += (_, ex) => captured = ex;

            await InvokePrivateAsync(view, "OnNewClickedAsync");
            Assert.NotNull(view.DataObject!.MasterRow);

            await InvokePrivateAsync(view, "OnSaveClickedAsync");

            Assert.NotNull(captured);
            Assert.Equal("backend rejected", captured!.Message);
        }

        private static void InvokeEditClicked(FormView view)
        {
            var method = typeof(FormView).GetMethod(
                "OnEditClicked", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(view, null);
        }

        private static TestFormView BuildInitializableView(Guid rowId, string name)
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, name) },
                GetDataHandler = _ => new GetDataResponse { DataSet = BuildServerDataSet(rowId, name) },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = BuildServerDataSet(Guid.NewGuid(), "Pending") },
            };
            return new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
        }

        [Fact]
        [DisplayName("初始為 View，選列載入後維持 View（唯讀瀏覽）")]
        public async Task FormMode_InitialAndAfterLoad_IsView()
        {
            var rowId = Guid.NewGuid();
            var view = BuildInitializableView(rowId, "Bob");
            await view.InitializeAsync();

            Assert.Equal(SingleFormMode.View, view.FormMode);

            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);

            Assert.Equal(SingleFormMode.View, view.FormMode);
        }

        [Fact]
        [DisplayName("Edit 鈕進入 Edit 模式；New 進入 Add 模式")]
        public async Task FormMode_EditAndNew_EnterEditingModes()
        {
            var rowId = Guid.NewGuid();
            var view = BuildInitializableView(rowId, "Bob");
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);

            InvokeEditClicked(view);
            Assert.Equal(SingleFormMode.Edit, view.FormMode);

            await InvokePrivateAsync(view, "OnNewClickedAsync");
            Assert.Equal(SingleFormMode.Add, view.FormMode);
        }

        [Fact]
        [DisplayName("Save 成功回到 View；Save 失敗停留在原模式")]
        public async Task FormMode_SaveOutcome_DrivesTransition()
        {
            var rowId = Guid.NewGuid();
            var saveShouldThrow = true;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "X") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = BuildServerDataSet(Guid.NewGuid(), "Pending") },
                SaveHandler = _ => saveShouldThrow
                    ? throw new InvalidOperationException("backend rejected")
                    : new SaveResponse(),
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();

            await InvokePrivateAsync(view, "OnNewClickedAsync");
            Assert.Equal(SingleFormMode.Add, view.FormMode);

            await InvokePrivateAsync(view, "OnSaveClickedAsync");
            Assert.Equal(SingleFormMode.Add, view.FormMode);

            saveShouldThrow = false;
            await InvokePrivateAsync(view, "OnSaveClickedAsync");
            Assert.Equal(SingleFormMode.View, view.FormMode);
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 委派到 DeleteAsync 並回到 View 模式")]
        public async Task OnDeleteClickedAsync_DelegatesToDeleteAsync()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            Guid? deletedRowId = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "Alice") },
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "Alice") },
                DeleteHandler = id => { deletedRowId = id; return new DeleteResponse(); },
            };
            var view = new TestFormView { Schema = schema, FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);

            await InvokePrivateAsync(view, "OnDeleteClickedAsync");

            Assert.Equal(rowId, deletedRowId);
            Assert.Equal(SingleFormMode.View, view.FormMode);
        }

        [Fact]
        [DisplayName("Delete 拋出例外時 ErrorOccurred 帶出例外訊息")]
        public async Task ErrorOccurred_FiresWhenDeleteThrows()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "X") },
                GetDataHandler = id => new GetDataResponse { DataSet = BuildServerDataSet(id, "X") },
                DeleteHandler = _ => throw new InvalidOperationException("delete rejected"),
            };
            var view = new TestFormView { Schema = schema, FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);

            Exception? captured = null;
            view.ErrorOccurred += (_, ex) => captured = ex;

            await InvokePrivateAsync(view, "OnDeleteClickedAsync");

            Assert.NotNull(captured);
            Assert.Equal("delete rejected", captured!.Message);
        }

        [Fact]
        [DisplayName("ListFields 為 null 時 ComputeSelectFields 只回傳 sys_rowid")]
        public async Task ComputeSelectFields_NullListFields_ReturnsJustRowId()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            string? capturedSelectFields = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = fields =>
                {
                    capturedSelectFields = fields;
                    return new GetListResponse { Table = null };
                },
            };
            var view = new TestFormView { Schema = schema, FormConnector = connector };

            await view.InitializeAsync();

            Assert.Equal("sys_rowid", capturedSelectFields);
        }

        [Fact]
        [DisplayName("GetListAsync 拋出例外時 InitializeAsync 觸發 ErrorOccurred")]
        public void InitializeAsync_GetListThrows_ErrorOccurred()
        {
            var schema = BuildEmployeeSchema();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => throw new InvalidOperationException("list load failed"),
            };
            var view = new TestFormView();
            view.Schema = schema;

            Exception? captured = null;
            view.ErrorOccurred += (_, ex) => captured = ex;

            view.FormConnector = connector;

            Assert.NotNull(captured);
            Assert.Equal("list load failed", captured!.Message);
        }

        [Fact]
        [DisplayName("GetDataAsync 拋出例外時 OnRowSelectedAsync 觸發 ErrorOccurred")]
        public async Task OnRowSelectedAsync_LoadThrows_ErrorOccurred()
        {
            var schema = BuildEmployeeSchema();
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildEmployeeListTable(rowId, "X") },
                GetDataHandler = _ => throw new InvalidOperationException("load failed"),
            };
            var view = new TestFormView { Schema = schema, FormConnector = connector };
            await view.InitializeAsync();

            Exception? captured = null;
            view.ErrorOccurred += (_, ex) => captured = ex;

            await InvokePrivateAsync(view, "OnRowSelectedAsync", rowId);

            Assert.NotNull(captured);
            Assert.Equal("load failed", captured!.Message);
        }

        /// <summary>
        /// Overrides the <c>Resolve*</c> hooks so tests never read the process-wide
        /// <c>ClientInfo</c> statics; the unused <c>ResolveFormConnector</c> throws to
        /// flag any unexpected ProgId-fallback path.
        /// </summary>
        private sealed class TestFormView : FormView
        {
            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("ClientInfo fallback must not be reached in unit tests.");

            protected override Guid ResolveAccessToken() => Guid.Empty;
        }

        /// <summary>
        /// Same test double the FormDataObject tests use; overrides every virtual
        /// CRUD method on <see cref="FormApiConnector"/> so the base
        /// <c>LocalApiProvider</c> is never reached.
        /// </summary>
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
