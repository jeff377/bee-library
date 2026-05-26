using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Paging;
using Bee.Definition.Sorting;
using Bee.Web.Blazor.Wasm.Components;
using Bee.Web.Blazor.Wasm.DataObjects;
using Bee.Web.Blazor.Wasm.DependencyInjection;

namespace Bee.Web.Blazor.Wasm.UnitTests.Components
{
    /// <summary>
    /// 補強 <see cref="FormPage"/> OnSaveAsync 與 OnDeleteAsync 成功路徑的覆蓋率。
    /// 使用 FullFakeFormConnector 讓 SaveAsync / DeleteAsync 成功返回，
    /// 以覆蓋 RunGuardedAsync 內緊接著 ReloadListAsync 的呼叫路徑。
    /// </summary>
    public class FormPageSaveDeleteCoverageTests
    {
        private sealed class FullFakeFormConnector : FormApiConnector
        {
            public FullFakeFormConnector() : base(Guid.Empty, "TestProg") { }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                FilterNode? filter = null,
                SortFieldCollection? sortFields = null,
                PagingOptions? paging = null)
                => Task.FromResult(new GetListResponse { Table = new DataTable("Test") });

            public override Task<SaveResponse> SaveAsync(DataSet dataSet)
                => Task.FromResult(new SaveResponse { DataSet = null });

            public override Task<DeleteResponse> DeleteAsync(Guid rowId)
                => Task.FromResult(new DeleteResponse());
        }

        private sealed class FullFakeFactory : BeeApiConnectorFactory
        {
            public FullFakeFactory()
                : base(new BeeBlazorOptions().UseRemoteProvider("http://test-endpoint")) { }

            public override FormApiConnector CreateFormConnector(Guid accessToken, string progId)
                => new FullFakeFormConnector();

            public override SystemApiConnector CreateSystemConnector(Guid accessToken)
                => throw new InvalidOperationException("SystemApiConnector not needed in this test.");
        }

        private static readonly FieldInfo s_dataObjectField =
            typeof(FormPage).GetField("_dataObject", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_listRowsField =
            typeof(FormPage).GetField("_listRows", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_errorField =
            typeof(FormPage).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static FormPage CreatePageWithFactory()
        {
            var page = new FormPage();
            typeof(FormPage).GetProperty("ProgId", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(page, "TestProg");
            typeof(FormPage).GetProperty("Factory", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(page, new FullFakeFactory());
            return page;
        }

        private static FormDataObject CreateDataObjectWithConnector()
        {
            var schema = new FormSchema("TestProg", "TestProg");
            var connector = new FullFakeFormConnector();
            var dataObject = new FormDataObject(schema, connector);

            // 手動加入主資料表，讓 DeleteAsync 的 RequireMasterRowId() 能成功取得 sys_rowid
            var masterTable = new DataTable("TestProg");
            masterTable.Columns.Add(SysFields.RowId, typeof(Guid));
            var masterRow = masterTable.NewRow();
            masterRow[SysFields.RowId] = Guid.NewGuid();
            masterTable.Rows.Add(masterRow);
            dataObject.DataSet.Tables.Add(masterTable);

            return dataObject;
        }

        private static async Task InvokeMethodAsync(FormPage page, string methodName)
        {
            var method = typeof(FormPage).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            await task;
        }

        [Fact]
        [DisplayName("OnSaveAsync _dataObject 非 null 且 SaveAsync 成功時應呼叫 ReloadListAsync 並設定 _listRows")]
        public async Task OnSaveAsync_SaveSucceeds_ReloadsListAndSetsListRows()
        {
            var page = CreatePageWithFactory();
            s_dataObjectField.SetValue(page, CreateDataObjectWithConnector());

            await InvokeMethodAsync(page, "OnSaveAsync");

            Assert.Null(s_errorField.GetValue(page) as string);
            Assert.NotNull(s_listRowsField.GetValue(page) as DataTable);
        }

        [Fact]
        [DisplayName("OnDeleteAsync _dataObject 非 null 且 DeleteAsync 成功時應呼叫 ReloadListAsync 並設定 _listRows")]
        public async Task OnDeleteAsync_DeleteSucceeds_ReloadsListAndSetsListRows()
        {
            var page = CreatePageWithFactory();
            s_dataObjectField.SetValue(page, CreateDataObjectWithConnector());

            await InvokeMethodAsync(page, "OnDeleteAsync");

            Assert.Null(s_errorField.GetValue(page) as string);
            Assert.NotNull(s_listRowsField.GetValue(page) as DataTable);
        }
    }
}
