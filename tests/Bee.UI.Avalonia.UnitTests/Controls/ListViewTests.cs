using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia.Controls;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Views;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// Behaviour tests for <see cref="ListView"/>. Drives the view through
    /// <see cref="ListView.InitializeAsync"/> + a <see cref="FakeFormApiConnector"/> so the
    /// unit-test environment never needs an Avalonia visual tree nor a live JSON-RPC
    /// backend. A <see cref="TestListView"/> subclass overrides the <c>Resolve*</c> hooks so
    /// the static <c>ClientInfo</c> is never touched.
    /// </summary>
    public class ListViewTests
    {
        private const string TestProgId = "Category";

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            schema.ListFields = "sys_id,sys_name";
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("sys_id", "Category Code", FieldDbType.String);
            master.Fields.Add("sys_name", "Category Name", FieldDbType.String);
            return schema;
        }

        private static DataTable BuildListTable(Guid rowId, string name)
        {
            var table = new DataTable(TestProgId);
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add(rowId, "C001", name);
            return table;
        }

        private static TestListView BuildInitializableView(FakeFormApiConnector connector)
            => new() { Schema = BuildSchema(), FormConnector = connector };

        private static void InvokePrivate(ListView view, string methodName, params object[] args)
        {
            var method = typeof(ListView).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method!.Invoke(view, args);
        }

        private static async Task InvokePrivateAsync(ListView view, string methodName, params object[] args)
        {
            var method = typeof(ListView).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            await (Task)method!.Invoke(view, args)!;
        }

        private static T InvokePrivateFunc<T>(ListView view, string methodName, params object[] args)
        {
            var method = typeof(ListView).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return (T)method!.Invoke(view, args)!;
        }

        [Fact]
        [DisplayName("ListView 為 UserControl 子類別")]
        public void Type_IsUserControlSubclass()
        {
            Assert.True(typeof(UserControl).IsAssignableFrom(typeof(ListView)));
        }

        [Theory]
        [InlineData(nameof(ListView.ProgId), "ProgIdProperty")]
        [InlineData(nameof(ListView.AccessToken), "AccessTokenProperty")]
        [InlineData(nameof(ListView.Schema), "SchemaProperty")]
        [InlineData(nameof(ListView.FormConnector), "FormConnectorProperty")]
        [DisplayName("公開屬性皆有對應的 StyledProperty 註冊")]
        public void PublicProperties_HaveMatchingStyledProperty(string propertyName, string styledPropertyFieldName)
        {
            var property = typeof(ListView).GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);

            var styled = typeof(ListView).GetField(
                styledPropertyFieldName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(styled);
        }

        [Fact]
        [DisplayName("InitializeAsync 透過 GetList 載入清單，SelectFields 帶 sys_rowid 前綴")]
        public async Task InitializeAsync_LoadsListWithRowIdPrefixedSelectFields()
        {
            string? requestedSelectFields = null;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = select =>
                {
                    requestedSelectFields = select;
                    return new GetListResponse { Table = BuildListTable(Guid.NewGuid(), "Beverages") };
                },
            };
            var view = BuildInitializableView(connector);

            await view.InitializeAsync();

            Assert.NotNull(requestedSelectFields);
            var fields = requestedSelectFields!.Split(',');
            Assert.Equal(SysFields.RowId, fields[0]);
            Assert.Contains("sys_id", fields);
            Assert.Contains("sys_name", fields);
        }

        [Fact]
        [DisplayName("ComputeSelectFields 不重複 sys_rowid")]
        public void ComputeSelectFields_DoesNotDuplicateRowId()
        {
            var schema = BuildSchema();
            schema.ListFields = "sys_rowid,sys_id,sys_name";
            var view = new TestListView { Schema = schema, FormConnector = new FakeFormApiConnector() };

            var result = InvokePrivateFunc<string>(view, "ComputeSelectFields");

            var occurrences = result.Split(',').Count(f => f.Equals(SysFields.RowId, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(1, occurrences);
        }

        [Fact]
        [DisplayName("選取列後按 View 觸發 ViewRequested 並帶正確 rowId")]
        public async Task View_AfterRowSelected_RaisesViewRequested()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildListTable(rowId, "Beverages") },
            };
            var view = BuildInitializableView(connector);
            await view.InitializeAsync();

            Guid requested = Guid.Empty;
            view.ViewRequested += (_, id) => requested = id;

            InvokePrivate(view, "OnRowSelected", rowId);
            InvokePrivate(view, "OnViewClicked");

            Assert.Equal(rowId, requested);
        }

        [Fact]
        [DisplayName("選取列後按 Edit 觸發 EditRequested 並帶正確 rowId")]
        public async Task Edit_AfterRowSelected_RaisesEditRequested()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildListTable(rowId, "Beverages") },
            };
            var view = BuildInitializableView(connector);
            await view.InitializeAsync();

            Guid requested = Guid.Empty;
            view.EditRequested += (_, id) => requested = id;

            InvokePrivate(view, "OnRowSelected", rowId);
            InvokePrivate(view, "OnEditClicked");

            Assert.Equal(rowId, requested);
        }

        [Fact]
        [DisplayName("選取列後雙擊觸發 ViewRequested（開啟唯讀檢視，不進入編輯）")]
        public async Task DoubleTap_AfterRowSelected_RaisesViewRequested()
        {
            var rowId = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildListTable(rowId, "Beverages") },
            };
            var view = BuildInitializableView(connector);
            await view.InitializeAsync();

            Guid viewRequested = Guid.Empty;
            Guid editRequested = Guid.Empty;
            view.ViewRequested += (_, id) => viewRequested = id;
            view.EditRequested += (_, id) => editRequested = id;

            InvokePrivate(view, "OnRowSelected", rowId);
            InvokePrivate(view, "OnGridDoubleTapped");

            Assert.Equal(rowId, viewRequested);
            Assert.Equal(Guid.Empty, editRequested);
        }

        [Fact]
        [DisplayName("未選取列時 Edit 不觸發 EditRequested")]
        public async Task Edit_WithoutSelection_DoesNotRaiseEditRequested()
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildListTable(Guid.NewGuid(), "Beverages") },
            };
            var view = BuildInitializableView(connector);
            await view.InitializeAsync();

            var raised = false;
            view.EditRequested += (_, _) => raised = true;

            InvokePrivate(view, "OnEditClicked");

            Assert.False(raised);
        }

        [Fact]
        [DisplayName("New 觸發 AddRequested")]
        public async Task New_RaisesAddRequested()
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = BuildListTable(Guid.NewGuid(), "Beverages") },
            };
            var view = BuildInitializableView(connector);
            await view.InitializeAsync();

            var raised = false;
            view.AddRequested += (_, _) => raised = true;

            InvokePrivate(view, "OnNewClicked");

            Assert.True(raised);
        }

        [Fact]
        [DisplayName("選取列後 Delete 呼叫 DeleteAsync 並重載清單")]
        public async Task Delete_AfterRowSelected_DeletesAndReloads()
        {
            var rowId = Guid.NewGuid();
            var getListCount = 0;
            Guid deletedRowId = Guid.Empty;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ =>
                {
                    getListCount++;
                    return new GetListResponse { Table = BuildListTable(rowId, "Beverages") };
                },
                DeleteHandler = id =>
                {
                    deletedRowId = id;
                    return new DeleteResponse();
                },
            };
            var view = BuildInitializableView(connector);
            await view.InitializeAsync();
            Assert.Equal(1, getListCount);

            InvokePrivate(view, "OnRowSelected", rowId);
            await InvokePrivateAsync(view, "OnDeleteClickedAsync");

            Assert.Equal(rowId, deletedRowId);
            Assert.Equal(2, getListCount);
        }

        [Fact]
        [DisplayName("清單載入失敗觸發 ErrorOccurred")]
        public async Task ReloadAsync_OnFailure_RaisesErrorOccurred()
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => throw new InvalidOperationException("backend down"),
            };

            // Subscribe before the FormConnector setter triggers initialization: assigning
            // Schema + FormConnector raises the StyledProperty change handler, which kicks
            // off InitializeAsync — so the failing list load happens during the setter, not
            // the explicit InitializeAsync call below.
            var view = new TestListView { Schema = BuildSchema() };
            Exception? reported = null;
            view.ErrorOccurred += (_, ex) => reported = ex;
            view.FormConnector = connector;

            await view.InitializeAsync();

            Assert.IsType<InvalidOperationException>(reported);
        }

        /// <summary>
        /// Overrides the <c>Resolve*</c> hooks so tests never read the process-wide
        /// <c>ClientInfo</c> statics; <c>ResolveFormConnector</c> throws to flag any
        /// unexpected ProgId-fallback path.
        /// </summary>
        private sealed class TestListView : ListView
        {
            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("ClientInfo fallback must not be reached in unit tests.");

            protected override Guid ResolveAccessToken() => Guid.Empty;
        }

        /// <summary>
        /// Test double overriding every virtual round-trip on <see cref="FormApiConnector"/>
        /// so the base <c>LocalApiProvider</c> is never reached.
        /// </summary>
        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<string, GetListResponse>? GetListHandler { get; set; }
            public Func<Guid, DeleteResponse>? DeleteHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (_ => new GetListResponse()))(selectFields));

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult((DeleteHandler ?? (_ => new DeleteResponse()))(rowId));
        }
    }
}
