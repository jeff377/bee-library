using System.ComponentModel;
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
    /// 補強 <see cref="FormPage"/> 覆蓋率：ComputeSelectFields 各路徑、
    /// ReloadListAsync 失敗觸發 ErrorOccurred、第二次 InitializeAsync 為 no-op。
    /// </summary>
    [Collection("ClientInfo")]
    public class FormPageCoverageTests
    {
        private const string TestProgId = "Employee";

        private static FormSchema BuildEmployeeSchema(string? listFields = null)
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("sys_id", "Employee ID", FieldDbType.String);
            master.Fields.Add("sys_name", "Name", FieldDbType.String);
            schema.ListFields = listFields!;
            return schema;
        }

        private static string InvokeComputeSelectFields(FormPage page)
        {
            var method = typeof(FormPage).GetMethod(
                "ComputeSelectFields", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            return (string)method!.Invoke(page, null)!;
        }

        [Fact]
        [DisplayName("ComputeSelectFields 在 Schema 為 null 時回傳空字串")]
        public void ComputeSelectFields_NullSchema_ReturnsEmpty()
        {
            var page = new FormPage();

            var result = InvokeComputeSelectFields(page);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("ComputeSelectFields 在 ListFields 為 null 時僅回傳 sys_rowid")]
        public void ComputeSelectFields_NullListFields_ReturnsSysRowIdOnly()
        {
            var schema = BuildEmployeeSchema(null);
            var page = new FormPage { Schema = schema };

            var result = InvokeComputeSelectFields(page);

            Assert.Equal(SysFields.RowId, result);
        }

        [Fact]
        [DisplayName("ComputeSelectFields 有 ListFields 時前置 sys_rowid 並包含全部欄位")]
        public void ComputeSelectFields_WithListFields_PrependsSysRowId()
        {
            var schema = BuildEmployeeSchema("sys_id,sys_name");
            var page = new FormPage { Schema = schema };

            var result = InvokeComputeSelectFields(page);

            Assert.Equal("sys_rowid,sys_id,sys_name", result);
        }

        [Fact]
        [DisplayName("ComputeSelectFields 當 ListFields 已含 sys_rowid 時去重，不重複出現")]
        public void ComputeSelectFields_ListFieldsContainsSysRowId_Deduplicates()
        {
            var schema = BuildEmployeeSchema($"{SysFields.RowId},sys_name");
            var page = new FormPage { Schema = schema };

            var result = InvokeComputeSelectFields(page);

            var fields = result.Split(',');
            Assert.Single(fields, f => string.Equals(f, SysFields.RowId, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        [DisplayName("ReloadListAsync 拋例外時觸發 ErrorOccurred 且不崩潰")]
        public async Task ReloadListAsync_GetListThrows_FiresErrorOccurred()
        {
            var connector = new FakeFormApiConnector
            {
                GetListHandler = () => throw new InvalidOperationException("list load failed"),
            };
            var page = new FormPage { Schema = BuildEmployeeSchema("sys_id") };
            Exception? captured = null;
            page.ErrorOccurred += (_, ex) => captured = ex;
            page.FormConnector = connector;

            var exception = await Record.ExceptionAsync(page.InitializeAsync);

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
                GetListHandler = () => new GetListResponse(),
            };
            var page = new FormPage
            {
                Schema = BuildEmployeeSchema("sys_id"),
                FormConnector = connector,
            };
            await page.InitializeAsync();
            var firstDataObject = page.DataObject;
            Assert.NotNull(firstDataObject);

            await page.InitializeAsync();

            Assert.Same(firstDataObject, page.DataObject);
        }

        private sealed class FakeFormApiConnector : FormApiConnector
        {
            public FakeFormApiConnector() : base(Guid.NewGuid(), TestProgId) { }

            public Func<GetListResponse>? GetListHandler { get; set; }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                Bee.Definition.Filters.FilterNode? filter = null,
                Bee.Definition.Sorting.SortFieldCollection? sortFields = null,
                Bee.Definition.Paging.PagingOptions? paging = null)
                => Task.FromResult((GetListHandler ?? (() => new GetListResponse()))());
        }
    }
}
