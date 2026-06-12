using System.ComponentModel;
using System.Data;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls.Editors;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// LookupPanel 單元測試：layout 綁定、ReloadAsync 經 stub connector 的取數與
    /// SearchText 傳遞、選取 commit / cancel 事件、載入失敗不拋例外。
    /// </summary>
    public class LookupPanelTests
    {
        private static FormSchema BuildCustomerSchema()
        {
            var schema = new FormSchema("Customer", "客戶") { CategoryId = "company" };
            var table = schema.Tables!.Add("Customer", "客戶");
            table.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            table.Fields!.Add(new FormField(SysFields.Id, "客戶代碼", FieldDbType.String));
            table.Fields!.Add(new FormField(SysFields.Name, "客戶名稱", FieldDbType.String));
            return schema;
        }

        private static DataTable BuildLookupTable(params (Guid rowId, string id, string name)[] rows)
        {
            var table = new DataTable("Customer");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add(SysFields.Id, typeof(string));
            table.Columns.Add(SysFields.Name, typeof(string));
            foreach (var (rowId, id, name) in rows)
                table.Rows.Add(rowId, id, name);
            return table;
        }

        [Fact]
        [DisplayName("Bind 應以 GetLookupLayout 建立 grid 欄位（含隱藏 sys_rowid）")]
        public void Bind_BuildsLookupLayoutColumns()
        {
            var panel = new LookupPanel();

            panel.Bind(BuildCustomerSchema(), new StubConnector(BuildLookupTable()));

            Assert.NotNull(panel.Grid.Layout);
            Assert.Equal(3, panel.Grid.Layout!.Columns!.Count);
            Assert.Equal(SysFields.Id, panel.Grid.Layout.Columns[0].FieldName);
            Assert.False(panel.Grid.Layout.Columns[2].Visible);
        }

        [Fact]
        [DisplayName("ReloadAsync 應傳遞 SearchText 並以回應填入 grid")]
        public async Task ReloadAsync_PassesSearchTextAndPopulatesGrid()
        {
            var table = BuildLookupTable(
                (Guid.NewGuid(), "C001", "客戶甲"),
                (Guid.NewGuid(), "C002", "客戶乙"));
            var connector = new StubConnector(table);
            var panel = new LookupPanel();
            panel.Bind(BuildCustomerSchema(), connector);
            panel.SearchText = "甲";

            await panel.ReloadAsync();

            Assert.Equal("甲", connector.LastSearchText);
            Assert.NotNull(panel.Grid.DataTable);
            Assert.Equal(2, panel.Grid.DataTable!.Rows.Count);
        }

        [Fact]
        [DisplayName("Commit 無選取應為 no-op；有選取應以該列觸發 Committed")]
        public async Task Commit_RaisesCommittedOnlyWithSelection()
        {
            var rowId = Guid.NewGuid();
            var table = BuildLookupTable((rowId, "C001", "客戶甲"));
            var panel = new LookupPanel();
            panel.Bind(BuildCustomerSchema(), new StubConnector(table));
            await panel.ReloadAsync();

            DataRow? committed = null;
            panel.Committed += (_, row) => committed = row;

            panel.Commit();
            Assert.Null(committed);

            panel.Grid.SelectedItem = panel.Grid.DataTable!.DefaultView[0];
            panel.Commit();

            Assert.NotNull(committed);
            Assert.Equal(rowId, committed![SysFields.RowId]);
        }

        [Fact]
        [DisplayName("Cancel 應觸發 Cancelled")]
        public void Cancel_RaisesCancelled()
        {
            var panel = new LookupPanel();
            var cancelled = false;
            panel.Cancelled += (_, _) => cancelled = true;

            panel.Cancel();

            Assert.True(cancelled);
        }

        [Fact]
        [DisplayName("ReloadAsync 連線失敗應顯示錯誤而不拋例外")]
        public async Task ReloadAsync_ConnectorFailure_DoesNotThrow()
        {
            var panel = new LookupPanel();
            panel.Bind(BuildCustomerSchema(), new ThrowingConnector());

            var exception = await Record.ExceptionAsync(() => panel.ReloadAsync());

            Assert.Null(exception);
            Assert.Null(panel.Grid.DataTable);
        }

        private sealed class StubConnector : FormApiConnector
        {
            private readonly DataTable _table;

            public StubConnector(DataTable table) : base(Guid.NewGuid(), "Customer")
            {
                _table = table;
            }

            public string? LastSearchText { get; private set; }

            public override Task<GetLookupResponse> GetLookupAsync(
                string searchText = "",
                Bee.Definition.Paging.PagingOptions? paging = null)
            {
                LastSearchText = searchText;
                return Task.FromResult(new GetLookupResponse { Table = _table });
            }
        }

        private sealed class ThrowingConnector : FormApiConnector
        {
            public ThrowingConnector() : base(Guid.NewGuid(), "Customer") { }

            public override Task<GetLookupResponse> GetLookupAsync(
                string searchText = "",
                Bee.Definition.Paging.PagingOptions? paging = null)
                => throw new InvalidOperationException("boom");
        }
    }
}
