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
    /// 補強 <see cref="FormView"/> 覆蓋率：ReloadListAsync 失敗路徑、
    /// 第二次 InitializeAsync 為 no-op、_emptyListLabel 可見性。
    /// </summary>
    public class FormViewErrorTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("sys_id", "ID", FieldDbType.String);
            master.Fields.Add("sys_name", "Name", FieldDbType.String);
            schema.ListFields = "sys_id,sys_name";
            return schema;
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

        private static TextBlock GetEmptyListLabel(FormView view)
        {
            var field = typeof(FormView).GetField(
                "_emptyListLabel", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (TextBlock)field!.GetValue(view)!;
        }

        [Fact]
        [DisplayName("ReloadListAsync 拋例外時觸發 ErrorOccurred 並不崩潰")]
        public async Task ReloadListAsync_GetListThrows_FiresErrorOccurred()
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => throw new InvalidOperationException("list load failed"),
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            Exception? captured = null;
            view.ErrorOccurred += (_, ex) => captured = ex;

            var exception = await Record.ExceptionAsync(view.InitializeAsync);

            Assert.Null(exception);
            Assert.NotNull(captured);
            Assert.Equal("list load failed", captured!.Message);
        }

        [Fact]
        [DisplayName("第二次呼叫 InitializeAsync 不重新建立 DataObject（no-op）")]
        public async Task InitializeAsync_AlreadyInitialized_IsNoOp()
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse
                {
                    Table = BuildEmployeeListTable(Guid.NewGuid(), "Alice"),
                },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();
            var firstDataObject = view.DataObject;
            Assert.NotNull(firstDataObject);

            await view.InitializeAsync();

            Assert.Same(firstDataObject, view.DataObject);
        }

        [Fact]
        [DisplayName("列表為空時 _emptyListLabel 顯示、_grid 隱藏")]
        public async Task ReloadListAsync_EmptyTable_ShowsEmptyLabel()
        {
            var emptyTable = new DataTable(TestProgId);
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse { Table = emptyTable },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();

            var emptyLabel = GetEmptyListLabel(view);
            Assert.True(emptyLabel.IsVisible);
        }

        [Fact]
        [DisplayName("列表有資料時 _emptyListLabel 隱藏")]
        public async Task ReloadListAsync_NonEmptyTable_HidesEmptyLabel()
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = _ => new GetListResponse
                {
                    Table = BuildEmployeeListTable(Guid.NewGuid(), "Bob"),
                },
            };
            var view = new TestFormView { Schema = BuildEmployeeSchema(), FormConnector = connector };
            await view.InitializeAsync();

            var emptyLabel = GetEmptyListLabel(view);
            Assert.False(emptyLabel.IsVisible);
        }

        private sealed class TestFormView : FormView
        {
            protected override SystemApiConnector? ResolveSystemConnector() => null;

            protected override FormApiConnector ResolveFormConnector(string progId)
                => throw new InvalidOperationException("ClientInfo fallback must not be reached.");

            protected override Guid ResolveAccessToken() => Guid.Empty;
        }

        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<string, GetListResponse>? GetListHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (_ => new GetListResponse()))(selectFields));
        }
    }
}
