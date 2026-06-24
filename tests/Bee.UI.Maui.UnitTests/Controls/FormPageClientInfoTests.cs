using System.ComponentModel;
using System.Data;
using System.Reflection;
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Providers;
using Bee.Api.Core.Messages.Form;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Tests.Shared;
using Bee.UI.Core;
using Bee.UI.Maui.Controls;

namespace Bee.UI.Maui.UnitTests.Controls
{
    /// <summary>
    /// Phase 1d behaviour tests for <see cref="FormPage"/>. Exercises the fallback
    /// path that resolves <see cref="FormSchema"/>, <see cref="FormApiConnector"/>,
    /// and <c>AccessToken</c> from <see cref="ClientInfo"/> when the host leaves
    /// the corresponding bindable properties unset. The page's protected
    /// <c>Resolve*</c> hooks are overridden via <see cref="FormPageTestProbe"/>
    /// so the test never needs a real backend.
    /// </summary>
    /// <remarks>
    /// <see cref="ClientInfo"/> and <see cref="ApiClientInfo"/> hold process-wide
    /// statics; every test here wraps work in a <see cref="ClientInfoTestScope"/>
    /// and the class opts in to <c>[Collection("ClientInfo")]</c> so xUnit
    /// serialises against other classes that mutate the same statics.
    /// </remarks>
    [Collection("ClientInfo")]
    public class FormPageClientInfoTests
    {
        private const string TestProgId = "Employee";
        private static readonly object[] s_testProgIdArgs = [TestProgId];

        private static FormSchema BuildEmployeeSchema()
        {
            var schema = new FormSchema(TestProgId, TestProgId);
            var master = schema.Tables!.Add(TestProgId, TestProgId);
            master.Fields!.Add(SysFields.RowId, "Row Id", FieldDbType.Guid);
            master.Fields.Add("sys_id", "Employee ID", FieldDbType.String);
            master.Fields.Add("sys_name", "Name", FieldDbType.String);
            return schema;
        }

        private static DataTable BuildEmployeeListTable()
        {
            var table = new DataTable(TestProgId);
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add("sys_id", typeof(string));
            table.Columns.Add("sys_name", typeof(string));
            table.Rows.Add(Guid.NewGuid(), "E001", "Alice");
            return table;
        }

        [Fact]
        [DisplayName("Schema 為 null + ProgId 設好時 InitializeAsync 會從 ResolveSchemaAsync 取 schema")]
        public async Task InitializeAsync_NullSchema_ResolvesFromHook()
        {
            using var scope = new ClientInfoTestScope();
            ApiClientInfo.ConnectType = ConnectType.Local;

            var schema = BuildEmployeeSchema();
            var listTable = BuildEmployeeListTable();
            var formConnector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = listTable },
            };

            var page = new FormPageTestProbe(_ => Task.FromResult<FormSchema?>(schema), formConnector)
            {
                ProgId = TestProgId,
            };

            await page.InitializeAsync();

            Assert.Same(schema, page.Schema);
            Assert.Equal(1, page.ResolveSchemaCalls);
            Assert.NotNull(page.DataObject);
        }

        [Fact]
        [DisplayName("FormConnector 為 null + ProgId 設好時 InitializeAsync 會從 ResolveFormConnector 取得")]
        public async Task InitializeAsync_NullConnector_ResolvesFromHook()
        {
            using var scope = new ClientInfoTestScope();
            ApiClientInfo.ConnectType = ConnectType.Local;

            var schema = BuildEmployeeSchema();
            var listTable = BuildEmployeeListTable();
            var formConnector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = listTable },
            };

            // Host pre-supplied the schema, so only the connector is missing.
            var page = new FormPageTestProbe(schemaResolver: null, formConnector)
            {
                Schema = schema,
                ProgId = TestProgId,
            };

            await page.InitializeAsync();

            Assert.Same(formConnector, page.FormConnector);
            Assert.Equal(1, page.ResolveFormConnectorCalls);
        }

        [Fact]
        [DisplayName("AccessToken 為 Guid.Empty 時會 fallback 到 ClientInfo.AccessToken")]
        public async Task InitializeAsync_EmptyAccessToken_FallsBackToClientInfo()
        {
            using var scope = new ClientInfoTestScope();
            ApiClientInfo.ConnectType = ConnectType.Local;
            var expectedToken = Guid.NewGuid();
            scope.SetAccessToken(expectedToken);

            var schema = BuildEmployeeSchema();
            var listTable = BuildEmployeeListTable();
            var formConnector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = listTable },
            };
            var page = new FormPageTestProbe(schemaResolver: null, formConnector)
            {
                Schema = schema,
                ProgId = TestProgId,
            };

            await page.InitializeAsync();

            Assert.Equal(expectedToken, page.AccessToken);
        }

        [Fact]
        [DisplayName("AccessToken 已由 host 提供時不會再被 ClientInfo 覆蓋")]
        public async Task InitializeAsync_HostSuppliedToken_NotOverwritten()
        {
            using var scope = new ClientInfoTestScope();
            ApiClientInfo.ConnectType = ConnectType.Local;
            scope.SetAccessToken(Guid.NewGuid()); // ClientInfo has a token …

            var hostToken = Guid.NewGuid();
            var schema = BuildEmployeeSchema();
            var listTable = BuildEmployeeListTable();
            var formConnector = new FakeFormApiConnector
            {
                GetListHandler = () => new GetListResponse { Table = listTable },
            };
            var page = new FormPageTestProbe(schemaResolver: null, formConnector)
            {
                Schema = schema,
                ProgId = TestProgId,
                AccessToken = hostToken, // … but the host has its own.
            };

            await page.InitializeAsync();

            Assert.Equal(hostToken, page.AccessToken);
        }

        [Fact]
        [DisplayName("缺 ProgId 且 Schema/FormConnector 也缺時 InitializeAsync 為 no-op")]
        public async Task InitializeAsync_NoProgIdAndNoInputs_IsNoOp()
        {
            using var scope = new ClientInfoTestScope();

            var page = new FormPageTestProbe(schemaResolver: null, formConnector: null);

            await page.InitializeAsync();

            Assert.Null(page.DataObject);
            Assert.Null(page.Schema);
            Assert.Null(page.FormConnector);
            Assert.Equal(0, page.ResolveFormConnectorCalls);
            Assert.Equal(0, page.ResolveSchemaCalls);
        }

        [Fact]
        [DisplayName("Resolve hooks 拋出時錯誤會路由到 ErrorOccurred")]
        public async Task InitializeAsync_SchemaResolveThrows_ReportsError()
        {
            using var scope = new ClientInfoTestScope();
            ApiClientInfo.ConnectType = ConnectType.Local;

            var page = new FormPageTestProbe(
                _ => throw new InvalidOperationException("schema fetch failed"),
                formConnector: null)
            {
                ProgId = TestProgId,
            };

            Exception? captured = null;
            page.ErrorOccurred += (_, ex) => captured = ex;

            await page.InitializeAsync();

            Assert.NotNull(captured);
            Assert.Equal("schema fetch failed", captured!.Message);
            Assert.Null(page.Schema);
            Assert.Null(page.DataObject);
        }

        [Theory]
        [InlineData(ConnectType.Local)]
        [InlineData(ConnectType.Remote)]
        [DisplayName("預設 Resolve 路徑會委派到 ClientInfo，Local/Remote 兩種模式都可正確建立連線物件")]
        public void Default_Resolve_DelegatesToClientInfo(ConnectType connectType)
        {
            using var scope = new ClientInfoTestScope();
            ApiClientInfo.ConnectType = connectType;
            ApiClientInfo.Endpoint = connectType == ConnectType.Remote
                ? "http://localhost/jsonrpc/api"
                : string.Empty;

            var page = new FormPage();
            var resolveSchema = typeof(FormPage).GetMethod(
                "ResolveSchemaAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var resolveForm = typeof(FormPage).GetMethod(
                "ResolveFormConnector", BindingFlags.NonPublic | BindingFlags.Instance);
            var resolveToken = typeof(FormPage).GetMethod(
                "ResolveAccessToken", BindingFlags.NonPublic | BindingFlags.Instance);
            // ResolveSchemaAsync defaults to the cached ClientInfo.DefineAccess; its presence is
            // asserted but not invoked here (invoking would issue a real define round-trip).
            Assert.NotNull(resolveSchema);
            Assert.NotNull(resolveForm);
            Assert.NotNull(resolveToken);

            var form = (FormApiConnector)resolveForm!.Invoke(page, s_testProgIdArgs)!;
            Assert.Equal(TestProgId, form.ProgId);
            var token = (Guid)resolveToken!.Invoke(page, Array.Empty<object>())!;
            Assert.Equal(ClientInfo.AccessToken, token);

            var expectedProviderType = connectType == ConnectType.Local
                ? typeof(LocalApiProvider)
                : typeof(RemoteApiProvider);
            Assert.IsType(expectedProviderType, form.Provider);
        }

        /// <summary>
        /// <see cref="FormPage"/> subclass that lets a test inject the <see cref="FormSchema"/>
        /// and <see cref="FormApiConnector"/> that the fallback would otherwise pull from
        /// <see cref="ClientInfo"/>. Also counts <c>Resolve*</c> invocations so tests can assert
        /// the fallback was — or was not — exercised.
        /// </summary>
        private sealed class FormPageTestProbe : FormPage
        {
            private readonly Func<string, Task<FormSchema?>>? _schemaResolver;
            private readonly FormApiConnector? _formConnector;

            public FormPageTestProbe(Func<string, Task<FormSchema?>>? schemaResolver, FormApiConnector? formConnector)
            {
                _schemaResolver = schemaResolver;
                _formConnector = formConnector;
            }

            public int ResolveSchemaCalls { get; private set; }

            public int ResolveFormConnectorCalls { get; private set; }

            protected override Task<FormSchema?> ResolveSchemaAsync(string progId)
            {
                ResolveSchemaCalls++;
                return _schemaResolver?.Invoke(progId) ?? Task.FromResult<FormSchema?>(null);
            }

            protected override FormApiConnector ResolveFormConnector(string progId)
            {
                ResolveFormConnectorCalls++;
                return _formConnector
                    ?? throw new InvalidOperationException(
                        "FormPageTestProbe.ResolveFormConnector was invoked but no fake connector was supplied.");
            }
        }

        /// <summary>
        /// Same fake the existing <c>FormPageTests</c> uses; duplicated here so the
        /// two files stay independent. Overrides every virtual CRUD method on
        /// <see cref="FormApiConnector"/> so the base <c>LocalApiProvider</c> is
        /// never reached.
        /// </summary>
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
