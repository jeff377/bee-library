using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Definitions;
using Bee.Base.Serialization;
using Bee.Business.Form;
using Bee.Db;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Layouts;
using Bee.Definition.Language;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Api.Client.UnitTests.Customization
{
    /// <summary>
    /// End-to-end cover for the tenant customization chain: <c>st_company.customize_id</c> →
    /// <c>EnterCompany</c> → <c>SessionInfo.CustomizeId</c> → the customization actually being
    /// applied, without any test handing a customization code to the code under test.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The component-level customization tests all pass a code in by hand, which cannot catch the
    /// failure that matters most here: a consumer that never asks the session for one. These tests
    /// enter a company through the API and then assert on the two consumers that read the session
    /// themselves — the language overlay behind <c>FormSchemaLocalizer</c>, and the progId → business
    /// object binding.
    /// </para>
    /// <para>
    /// The schema is fetched and localized through <see cref="FormDefinitionLoader"/> rather than by
    /// calling the localizer directly, because that is where the two language layers meet in
    /// production: the server serves raw definitions and decides <b>which tenant's</b> customization
    /// layer to hand out, and the caller decides how the layers combine.
    /// </para>
    /// <para>
    /// Bound to the <c>ApiClientInfoState</c> collection: near-end connectors read the process-wide
    /// <c>ApiClientInfo.ConnectType</c>, which the other classes in that collection mutate.
    /// </para>
    /// </remarks>
    [Collection("ApiClientInfoState")]
    public class TenantCustomizationEndToEndTests : IClassFixture<TenantCustomizationFixture>
    {
        private const string SeedUserId = "001";

        // BO / session state resolves through the "common" databaseId, which the test rig binds to
        // SQL Server; the company-scoped permission tables live in the matching company database.
        private static readonly string CompanyDbId =
            TestDbConventions.GetDatabaseId(DatabaseType.SQLServer, "company");

        private static readonly string[] CustomerSchemaKeys = [TenantCustomizationFixture.ProgId];

        private readonly TenantCustomizationFixture _fx;

        public TenantCustomizationEndToEndTests(TenantCustomizationFixture fx) { _fx = fx; }

        // ---- 客製生效 ----

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("進入帶 customize_id 的公司後，取得的 schema 欄位 caption 應為客製值")]
        public async Task GetLocalizedSchema_AfterEnteringCustomizedCompany_UsesTenantText()
        {
            using var scope = await TenantScope.EnterAsync(this, TenantCustomizationFixture.CustomizeId);

            var schema = await LoadLocalizedSchemaAsync(scope.AccessToken);

            Assert.Equal(TenantCustomizationFixture.OverriddenCaption, CaptionOf(schema, TenantCustomizationFixture.OverriddenField));
            Assert.Equal(TenantCustomizationFixture.OverriddenSchemaDisplayName, schema.DisplayName);
            // The tenant file lists two sub-keys only; everything else must still resolve to base.
            Assert.Equal(TenantCustomizationFixture.InheritedCaption, CaptionOf(schema, TenantCustomizationFixture.InheritedField));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("進入帶 customize_id 的公司後，BO 型別應解析為客製 ProgramSettings 綁定的型別")]
        public async Task CreateFormBusinessObject_AfterEnteringCustomizedCompany_ResolvesTenantType()
        {
            using var scope = await TenantScope.EnterAsync(this, TenantCustomizationFixture.CustomizeId);

            var bo = _fx.GetRequiredService<IBusinessObjectFactory>()
                .CreateFormBusinessObject(scope.AccessToken, TenantCustomizationFixture.ProgId);

            Assert.IsType<TenantCustomerBusinessObject>(bo);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("離開公司後客製即失效，重新取得的 schema 應回到 base 文字")]
        public async Task GetLocalizedSchema_AfterLeavingCompany_FallsBackToBase()
        {
            using var scope = await TenantScope.EnterAsync(this, TenantCustomizationFixture.CustomizeId);
            var customized = await LoadLocalizedSchemaAsync(scope.AccessToken);
            Assert.Equal(TenantCustomizationFixture.OverriddenCaption, CaptionOf(customized, TenantCustomizationFixture.OverriddenField));

            await new SystemApiConnector(scope.AccessToken).LeaveCompanyAsync();

            // A fresh loader, because ClientDefineAccess caches per definition key and not per
            // tenant — dropping that cache on a tenant switch is the caller's job.
            var afterLeave = await LoadLocalizedSchemaAsync(scope.AccessToken);
            Assert.Equal(TenantCustomizationFixture.BaseCaption, CaptionOf(afterLeave, TenantCustomizationFixture.OverriddenField));
            Assert.Equal(TenantCustomizationFixture.BaseSchemaDisplayName, afterLeave.DisplayName);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("進入帶 customize_id 的公司後，執行階段 layout 應整檔採用客製定義，caption 取自在地化 schema")]
        public async Task GetRuntimeLayout_AfterEnteringCustomizedCompany_UsesTenantLayout()
        {
            using var scope = await TenantScope.EnterAsync(this, TenantCustomizationFixture.CustomizeId);
            var access = new ClientDefineAccess(new SystemApiConnector(scope.AccessToken));
            var loader = new FormDefinitionLoader(access);
            var schema = await loader.GetLocalizedSchemaAsync(TenantCustomizationFixture.ProgId, TenantCustomizationFixture.Lang);

            var layout = await loader.GetRuntimeLayoutAsync(TenantCustomizationFixture.ProgId, schema);

            var fields = layout.Sections![0].Fields!;
            // Whole-file replacement, not a merge: the base file's other five fields are gone.
            Assert.Equal(TenantCustomizationFixture.OverriddenLayoutFieldCount, fields.Count);
            // The layout file carries no captions, so this text can only have come from the
            // localized schema — which itself resolved through the tenant's language override.
            Assert.Equal(TenantCustomizationFixture.OverriddenCaption, LayoutCaptionOf(layout, TenantCustomizationFixture.OverriddenField));
        }

        // ---- 跨租戶隔離 ----

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("跨租戶隔離：另一個 customize_id 沒有客製檔時應取得純 base 結果")]
        public async Task GetLocalizedSchema_OtherTenantWithoutOverrides_MatchesBaseLayer()
        {
            using var scope = await TenantScope.EnterAsync(this, TenantCustomizationFixture.UncustomizedId);

            var schema = await LoadLocalizedSchemaAsync(scope.AccessToken);

            Assert.Equal(BaseLineXml(), XmlCodec.Serialize(schema));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("跨租戶隔離：另一個 customize_id 的 BO 型別應解析為框架預設 FormBusinessObject")]
        public async Task CreateFormBusinessObject_OtherTenantWithoutOverrides_ResolvesDefaultType()
        {
            using var scope = await TenantScope.EnterAsync(this, TenantCustomizationFixture.UncustomizedId);

            var bo = _fx.GetRequiredService<IBusinessObjectFactory>()
                .CreateFormBusinessObject(scope.AccessToken, TenantCustomizationFixture.ProgId);

            Assert.IsType<FormBusinessObject>(bo);
        }

        // ---- 回歸防護：未設 CustomizeId 的部署行為零變化 ----

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("回歸防護：未進公司的 session 取得的執行階段 layout 應來自 base 定義檔")]
        public async Task GetRuntimeLayout_SessionWithoutCompany_UsesBaseLayoutDefinition()
        {
            var sessions = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, SeedUserId);
            try
            {
                var access = new ClientDefineAccess(new SystemApiConnector(accessToken));
                var loader = new FormDefinitionLoader(access);
                var schema = await loader.GetLocalizedSchemaAsync(TenantCustomizationFixture.ProgId, TenantCustomizationFixture.Lang);

                var layout = await loader.GetRuntimeLayoutAsync(TenantCustomizationFixture.ProgId, schema);

                Assert.Equal(BaseLayoutFieldCount(), layout.Sections![0].Fields!.Count);
                Assert.Equal(TenantCustomizationFixture.BaseCaption,
                    LayoutCaptionOf(layout, TenantCustomizationFixture.OverriddenField));
            }
            finally
            {
                sessions.Remove(accessToken);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("回歸防護：未進公司的 session 取得的 schema 應與純 base 逐位元一致")]
        public async Task GetLocalizedSchema_SessionWithoutCompany_MatchesBaseLayerByteForByte()
        {
            var sessions = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, SeedUserId);
            try
            {
                var schema = await LoadLocalizedSchemaAsync(accessToken);

                Assert.Equal(BaseLineXml(), XmlCodec.Serialize(schema));
            }
            finally
            {
                sessions.Remove(accessToken);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("回歸防護：未進公司的 session 應解析為框架預設 FormBusinessObject")]
        public void CreateFormBusinessObject_SessionWithoutCompany_ResolvesDefaultType()
        {
            var sessions = _fx.GetRequiredService<ISessionInfoService>();
            var accessToken = TestSessionFactory.CreateAccessToken(_fx, SeedUserId);
            try
            {
                var bo = _fx.GetRequiredService<IBusinessObjectFactory>()
                    .CreateFormBusinessObject(accessToken, TenantCustomizationFixture.ProgId);

                Assert.IsType<FormBusinessObject>(bo);
            }
            finally
            {
                sessions.Remove(accessToken);
            }
        }

        // ---- Helpers ----

        private static async Task<FormSchema> LoadLocalizedSchemaAsync(Guid accessToken)
        {
            var access = new ClientDefineAccess(new SystemApiConnector(accessToken));
            return await new FormDefinitionLoader(access)
                .GetLocalizedSchemaAsync(TenantCustomizationFixture.ProgId, TenantCustomizationFixture.Lang);
        }

        private static string CaptionOf(FormSchema schema, string fieldName)
            => schema.Tables![TenantCustomizationFixture.ProgId].Fields![fieldName].Caption;

        // Layout field collections are ordered, not keyed — a layout may repeat or omit fields.
        private static string LayoutCaptionOf(FormLayout layout, string fieldName)
            => layout.Sections![0].Fields!.Single(f => f.FieldName == fieldName).Caption;

        /// <summary>
        /// The schema localized against the base layer alone, serialized. Built here rather than
        /// captured as a literal so "unchanged" means "unchanged from what the base layer produces
        /// today", not "unchanged from what someone typed into this file once".
        /// </summary>
        private string BaseLineXml()
        {
            var schema = ((FormSchema)_fx.GetRequiredService<IDefineAccess>()
                .GetDefine(DefineType.FormSchema, CustomerSchemaKeys)!).Clone();
            new FormSchemaLocalizer(_fx.GetRequiredService<ILanguageService>())
                .Localize(schema, TenantCustomizationFixture.Lang);
            return XmlCodec.Serialize(schema);
        }

        /// <summary>
        /// The field count of the base layout definition file, read rather than hard-coded so this
        /// assertion keeps meaning "same as base" if the shared fixture file is ever edited.
        /// </summary>
        private int BaseLayoutFieldCount()
        {
            var layout = _fx.GetRequiredService<IDefineAccess>()
                .FindFormLayout(string.Empty, TenantCustomizationFixture.ProgId)
                ?? throw new InvalidOperationException($"Base FormLayout '{TenantCustomizationFixture.ProgId}' not found.");
            return layout.Sections![0].Fields!.Count;
        }

        private DbAccess Common() => _fx.NewDbAccess("common");

        private Guid InsertCompany(string companyId, string customizeId)
        {
            var rowId = Guid.NewGuid();
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_company (sys_rowid, sys_id, sys_name, company_database_id, customize_id, " +
                "number_formats_xml, default_currency, cash_rounding_xml, allowed_currencies_xml, enabled, sys_insert_time) " +
                "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, 1, GETUTCDATE())",
                rowId, companyId, "客製化端到端測試公司", CompanyDbId, customizeId,
                string.Empty, string.Empty, string.Empty, string.Empty);
            Common().Execute(insert);
            return rowId;
        }

        private Guid InsertGrant(Guid userRowId, Guid companyRowId)
        {
            var rowId = Guid.NewGuid();
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_user_company (sys_rowid, user_rowid, company_rowid, sys_insert_time) " +
                "VALUES ({0}, {1}, {2}, GETUTCDATE())",
                rowId, userRowId, companyRowId);
            Common().Execute(insert);
            return rowId;
        }

        private void DeleteRow(string tableName, Guid rowId)
        {
            var delete = new DbCommandSpec(DbCommandKind.NonQuery,
                $"DELETE FROM {tableName} WHERE sys_rowid = {{0}}", rowId);
            Common().Execute(delete);
        }

        private Guid LookupSeedUserRowId()
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT sys_rowid FROM st_user WHERE sys_id = {0}", SeedUserId);
            var value = Common().Execute(spec).Scalar;
            if (value is Guid guid) return guid;
            if (value is byte[] bytes && bytes.Length == 16) return new Guid(bytes);
            if (value is string text && Guid.TryParse(text, out var parsed)) return parsed;
            throw new InvalidOperationException($"Cannot resolve user rowid for '{SeedUserId}'.");
        }

        /// <summary>
        /// A signed-in session that has entered a freshly seeded company carrying the supplied
        /// customization code. Seeds the company row and the user's access grant, enters the company
        /// through the API, and undoes all three on disposal.
        /// </summary>
        /// <remarks>
        /// Nested and private because it only makes sense against this class's helpers; it exists to
        /// keep each test down to the arrangement that distinguishes it from the others.
        /// </remarks>
        private sealed class TenantScope : IDisposable
        {
            private readonly TenantCustomizationEndToEndTests _tests;
            private readonly Guid _companyRowId;
            private readonly Guid _grantRowId;

            private TenantScope(TenantCustomizationEndToEndTests tests, Guid accessToken, Guid companyRowId, Guid grantRowId)
            {
                _tests = tests;
                AccessToken = accessToken;
                _companyRowId = companyRowId;
                _grantRowId = grantRowId;
            }

            /// <summary>Gets the access token of the session that entered the company.</summary>
            public Guid AccessToken { get; }

            public static async Task<TenantScope> EnterAsync(TenantCustomizationEndToEndTests tests, string customizeId)
            {
                var companyId = "E2E" + Guid.NewGuid().ToString("N")[..6];
                var companyRowId = tests.InsertCompany(companyId, customizeId);
                var grantRowId = tests.InsertGrant(tests.LookupSeedUserRowId(), companyRowId);
                var accessToken = TestSessionFactory.CreateAccessToken(tests._fx, SeedUserId);

                bool entered = false;
                try
                {
                    await new SystemApiConnector(accessToken).EnterCompanyAsync(companyId);
                    entered = true;
                }
                finally
                {
                    if (!entered) Cleanup(tests, accessToken, companyRowId, grantRowId);
                }
                return new TenantScope(tests, accessToken, companyRowId, grantRowId);
            }

            public void Dispose() => Cleanup(_tests, AccessToken, _companyRowId, _grantRowId);

            private static void Cleanup(
                TenantCustomizationEndToEndTests tests, Guid accessToken, Guid companyRowId, Guid grantRowId)
            {
                tests._fx.GetRequiredService<ISessionInfoService>().Remove(accessToken);
                tests.DeleteRow("st_user_company", grantRowId);
                tests.DeleteRow("st_company", companyRowId);
            }
        }
    }
}
