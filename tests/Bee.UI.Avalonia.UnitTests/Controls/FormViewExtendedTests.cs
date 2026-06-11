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
    /// 補強 <see cref="FormView"/> 的覆蓋率：ComputeSelectFields 各情境、
    /// ApplyAccessTokenFallback、Delete 流程、UpdateToolbarState。
    /// </summary>
    public class FormViewExtendedTests
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

        private static async Task InvokePrivateAsync(FormView view, string methodName)
        {
            var method = typeof(FormView).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            await (Task)method!.Invoke(view, null)!;
        }

        private static string InvokeComputeSelectFields(FormView view)
        {
            var method = typeof(FormView).GetMethod(
                "ComputeSelectFields", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return (string)method!.Invoke(view, null)!;
        }

        private static TField GetPrivateField<TField>(FormView view, string fieldName)
        {
            var field = typeof(FormView).GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (TField)field!.GetValue(view)!;
        }

        [Fact]
        [DisplayName("ComputeSelectFields Schema 為 null 時回傳空字串")]
        public void ComputeSelectFields_NullSchema_ReturnsEmpty()
        {
            var view = new TestFormView();

            var result = InvokeComputeSelectFields(view);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("ComputeSelectFields ListFields 為 null 時只回傳 sys_rowid")]
        public void ComputeSelectFields_NullListFields_ReturnsSysRowIdOnly()
        {
            var schema = new FormSchema("Emp", "Emp");
            var view = new TestFormView { Schema = schema };

            var result = InvokeComputeSelectFields(view);

            Assert.Equal(SysFields.RowId, result);
        }

        [Fact]
        [DisplayName("ComputeSelectFields ListFields 已含 sys_rowid 時不重複加入")]
        public void ComputeSelectFields_ListFieldsIncludesSysRowId_Deduplicates()
        {
            var schema = new FormSchema("Emp", "Emp");
            schema.ListFields = "sys_rowid,sys_id";
            var view = new TestFormView { Schema = schema };

            var result = InvokeComputeSelectFields(view);

            Assert.Equal("sys_rowid,sys_id", result);
        }

        [Fact]
        [DisplayName("AccessToken 為空時以 ResolveAccessToken 的回傳值填入")]
        public async Task ApplyAccessTokenFallback_WithNonEmptyResolvedToken_SetsAccessToken()
        {
            var expectedToken = Guid.NewGuid();
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse
                    { Table = BuildEmployeeListTable(Guid.NewGuid(), "X") },
            };
            var view = new TestFormViewWithToken(expectedToken)
            {
                Schema = BuildEmployeeSchema(),
                FormConnector = connector,
            };

            await view.InitializeAsync();

            Assert.Equal(expectedToken, view.AccessToken);
        }

        [Fact]
        [DisplayName("New 之後 Save / Delete 按鈕啟用")]
        public async Task UpdateToolbarState_AfterNewWithMaster_EnablesSaveAndDelete()
        {
            var rowId = Guid.NewGuid();
            var newDataSet = BuildServerDataSet(rowId, "New Employee");
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse
                    { Table = BuildEmployeeListTable(Guid.NewGuid(), "X") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = newDataSet },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();

            await InvokePrivateAsync(view, "OnNewClickedAsync");

            var saveButton = GetPrivateField<Button>(view, "_saveButton");
            var deleteButton = GetPrivateField<Button>(view, "_deleteButton");
            Assert.True(saveButton.IsEnabled);
            Assert.True(deleteButton.IsEnabled);
        }

        [Fact]
        [DisplayName("IsDirty 為 true 時 dirty marker 可見")]
        public async Task UpdateToolbarState_DirtyDataObject_ShowsDirtyMarker()
        {
            var rowId = Guid.NewGuid();
            var newDataSet = BuildServerDataSet(rowId, "New Employee");
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse
                    { Table = BuildEmployeeListTable(Guid.NewGuid(), "X") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = newDataSet },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnNewClickedAsync");

            view.DataObject!.SetField("sys_name", "Changed");

            var updateState = typeof(FormView).GetMethod(
                "UpdateToolbarState", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(updateState);
            updateState!.Invoke(view, null);

            var dirtyMarker = GetPrivateField<TextBlock>(view, "_dirtyMarker");
            Assert.True(dirtyMarker.IsVisible);
        }

        [Fact]
        [DisplayName("OnDeleteClickedAsync 委派給 FormDataObject.DeleteAsync")]
        public async Task OnDeleteClickedAsync_DelegatesToDeleteAsync()
        {
            var rowId = Guid.NewGuid();
            var newDataSet = BuildServerDataSet(rowId, "New Employee");
            var deletedId = Guid.Empty;
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse
                    { Table = BuildEmployeeListTable(rowId, "X") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = newDataSet },
                DeleteHandler = id =>
                {
                    deletedId = id;
                    return new DeleteResponse();
                },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnNewClickedAsync");

            await InvokePrivateAsync(view, "OnDeleteClickedAsync");

            Assert.Equal(rowId, deletedId);
        }

        [Fact]
        [DisplayName("Delete 失敗時 ErrorOccurred 帶出例外訊息")]
        public async Task ErrorOccurred_FiresWhenDeleteThrows()
        {
            var rowId = Guid.NewGuid();
            var newDataSet = BuildServerDataSet(rowId, "New Employee");
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse
                    { Table = BuildEmployeeListTable(rowId, "X") },
                GetNewDataHandler = () => new GetNewDataResponse { DataSet = newDataSet },
                DeleteHandler = _ => throw new InvalidOperationException("delete rejected"),
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            await InvokePrivateAsync(view, "OnNewClickedAsync");

            Exception? captured = null;
            view.ErrorOccurred += (_, ex) => captured = ex;

            await InvokePrivateAsync(view, "OnDeleteClickedAsync");

            Assert.NotNull(captured);
            Assert.Equal("delete rejected", captured!.Message);
        }

        private sealed class TestFormView : FormView
        {
            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("ClientInfo fallback must not be reached in unit tests.");

            protected override Guid ResolveAccessToken() => Guid.Empty;
        }

        private sealed class TestFormViewWithToken : FormView
        {
            private readonly Guid _token;

            public TestFormViewWithToken(Guid token) { _token = token; }

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
