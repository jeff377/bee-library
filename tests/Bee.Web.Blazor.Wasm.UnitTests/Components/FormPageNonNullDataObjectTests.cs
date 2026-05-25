using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.Form;
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
    /// 補強 <see cref="FormPage"/> 四個 Action handler 在 <c>_dataObject</c> 非 null 時
    /// 的覆蓋率，以及 <c>ReloadListAsync</c> 的覆蓋率。
    /// Action handler 以無 connector 的 <see cref="FormDataObject"/> 觸發例外，
    /// 例外由 <c>RunGuardedAsync</c> 捕捉並設定 <c>_error</c>；
    /// <c>ReloadListAsync</c> 由注入的 <see cref="FakeFactory"/> 驅動完整路徑。
    /// </summary>
    public class FormPageNonNullDataObjectTests
    {
        private sealed class FakeFormConnector : FormApiConnector
        {
            public FakeFormConnector() : base(Guid.Empty, "TestProg") { }

            public override Task<GetListResponse> GetListAsync(
                string selectFields = "",
                FilterNode? filter = null,
                SortFieldCollection? sortFields = null,
                PagingOptions? paging = null)
                => Task.FromResult(new GetListResponse { Table = new DataTable("Test") });
        }

        private sealed class FakeFactory : BeeApiConnectorFactory
        {
            public FakeFactory()
                : base(new BeeBlazorOptions().UseRemoteProvider("http://test-endpoint")) { }

            public override FormApiConnector CreateFormConnector(Guid accessToken, string progId)
                => new FakeFormConnector();

            public override SystemApiConnector CreateSystemConnector(Guid accessToken)
                => throw new InvalidOperationException("SystemApiConnector not needed in this test.");
        }

        private static readonly FieldInfo s_dataObjectField =
            typeof(FormPage).GetField("_dataObject", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_errorField =
            typeof(FormPage).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly FieldInfo s_listRowsField =
            typeof(FormPage).GetField("_listRows", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static FormDataObject CreateDataObjectWithoutConnector()
        {
            var schema = new FormSchema("Test", "Test");
            return new FormDataObject(schema);
        }

        private static async Task InvokeMethodAsync(FormPage page, string methodName, object[]? args = null)
        {
            var method = typeof(FormPage).GetMethod(
                methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, args)!;
            await task;
        }

        private static FormPage CreatePageWithDataObject()
        {
            var page = new FormPage();
            s_dataObjectField.SetValue(page, CreateDataObjectWithoutConnector());
            return page;
        }

        [Fact]
        [DisplayName("OnRowSelectedAsync _dataObject 非 null 且無 connector 時應透過 RunGuardedAsync 捕捉例外並設定 _error")]
        public async Task OnRowSelectedAsync_NonNullDataObjectNoConnector_SetsError()
        {
            var page = CreatePageWithDataObject();
            await InvokeMethodAsync(page, "OnRowSelectedAsync", new object[] { Guid.NewGuid() });
            Assert.NotNull(s_errorField.GetValue(page) as string);
        }

        [Fact]
        [DisplayName("OnNewAsync _dataObject 非 null 且無 connector 時應透過 RunGuardedAsync 捕捉例外並設定 _error")]
        public async Task OnNewAsync_NonNullDataObjectNoConnector_SetsError()
        {
            var page = CreatePageWithDataObject();
            await InvokeMethodAsync(page, "OnNewAsync", null);
            Assert.NotNull(s_errorField.GetValue(page) as string);
        }

        [Fact]
        [DisplayName("OnSaveAsync _dataObject 非 null 且無 connector 時應透過 RunGuardedAsync 捕捉例外並設定 _error")]
        public async Task OnSaveAsync_NonNullDataObjectNoConnector_SetsError()
        {
            var page = CreatePageWithDataObject();
            await InvokeMethodAsync(page, "OnSaveAsync", null);
            Assert.NotNull(s_errorField.GetValue(page) as string);
        }

        [Fact]
        [DisplayName("OnDeleteAsync _dataObject 非 null 且無 connector 時應透過 RunGuardedAsync 捕捉例外並設定 _error")]
        public async Task OnDeleteAsync_NonNullDataObjectNoConnector_SetsError()
        {
            var page = CreatePageWithDataObject();
            await InvokeMethodAsync(page, "OnDeleteAsync", null);
            Assert.NotNull(s_errorField.GetValue(page) as string);
        }

        [Fact]
        [DisplayName("ReloadListAsync 使用 FakeFactory 應成功設定 _listRows 為非 null DataTable")]
        public async Task ReloadListAsync_WithFakeFactory_SetsListRows()
        {
            var page = new FormPage();
            typeof(FormPage).GetProperty("ProgId", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(page, "TestProg");
            typeof(FormPage).GetProperty("Factory", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(page, new FakeFactory());

            var method = typeof(FormPage).GetMethod(
                "ReloadListAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var task = (Task)method!.Invoke(page, null)!;
            await task;

            Assert.NotNull(s_listRowsField.GetValue(page) as DataTable);
        }
    }
}
