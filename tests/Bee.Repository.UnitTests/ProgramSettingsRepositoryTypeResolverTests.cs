using System.ComponentModel;
using Bee.Base;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Factories;
using Bee.Repository.Form;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="ProgramSettingsRepositoryTypeResolver"/>：<c>ProgramItem.Repository</c> 的四條解析路徑
    /// （有值 / 無值 / 型別載不到 / 型別非衍生）與租戶客製 overlay 的取代行為。
    /// </summary>
    /// <remarks>
    /// 這些測試原本以工廠為對象（<c>ProgramItemRepositoryBindingTests</c>），為了建工廠得備妥
    /// 資料庫存取、連線、路由三個永遠不會被呼叫的 stub。解析抽成獨立型別後直接測它。
    /// 例外訊息的斷言原封搬過來；型別斷言從「建出的實例是某型別」改成「解析出的型別是某型別」，
    /// 因為受測對象現在回傳的是 <see cref="Type"/>。工廠依解析結果建出實例、帶上 progId 的那一半
    /// 見 <see cref="RepositoryFactoryGuardTests"/>。
    /// </remarks>
    public class ProgramSettingsRepositoryTypeResolverTests
    {
        private const string ProgId = "Employee";
        private const string CustomizeId = "acme";

        #region Stubs 與測試用 repository

        /// <summary>綁定成功時應解析出的自訂 repository。</summary>
        public class CustomEmployeeRepository : DataFormRepository
        {
            public CustomEmployeeRepository(IRepositoryContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId)
            {
            }
        }

        /// <summary>租戶客製層應解析出的 repository，用來證明客製項整筆取代基底項。</summary>
        public class TenantEmployeeRepository : DataFormRepository
        {
            public TenantEmployeeRepository(IRepositoryContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId)
            {
            }
        }

        /// <summary>不衍生自 <see cref="DataFormRepository"/>，用來驗證契約檢查。</summary>
        public class NotARepository
        {
        }

        private sealed class StubDefineAccess : IDefineAccess
        {
            public ProgramSettings? Programs { get; set; }

            public ProgramSettings GetProgramSettings()
                => Programs ?? throw new FileNotFoundException("ProgramSettings.xml");

            public FormSchema GetFormSchema(string progId) => throw new NotImplementedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotImplementedException();
            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotImplementedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
            public LanguageResource GetLanguage(string lang, string ns) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }

        /// <summary>只認得一組 (customizeId → ProgramSettings)。</summary>
        private sealed class StubCustomizeReader : ICustomizeDefineReader
        {
            private readonly string _customizeId;
            private readonly ProgramSettings _settings;

            public StubCustomizeReader(string customizeId, ProgramSettings settings)
            {
                _customizeId = customizeId;
                _settings = settings;
            }

            public ProgramSettings? GetCustomizeProgramSettings(string customizeId)
                => StringUtilities.IsEquals(customizeId, _customizeId) ? _settings : null;

            public LanguageResource? GetCustomizeLanguage(string customizeId, string lang, string ns) => null;
            public MenuSettings? GetCustomizeMenuSettings(string customizeId) => null;
            public PluginSettings? GetCustomizePluginSettings(string customizeId) => null;
            public FormLayout? GetCustomizeFormLayout(string customizeId, string layoutId) => null;
        }

        /// <summary>對指定權杖回傳帶 CustomizeId 的 session，其餘回 null。</summary>
        private sealed class StubSessionInfoService : ISessionInfoService
        {
            private readonly Guid _token;
            private readonly string _customizeId;

            public StubSessionInfoService(Guid token, string customizeId)
            {
                _token = token;
                _customizeId = customizeId;
            }

            public SessionInfo Get(Guid accessToken)
                => accessToken == _token
                    ? new SessionInfo { AccessToken = accessToken, CustomizeId = _customizeId }
                    : null!;

            public void Set(SessionInfo sessionInfo) => throw new NotSupportedException();
            public void Remove(Guid accessToken) => throw new NotSupportedException();
        }

        private static ProgramSettings Registry(string? repositoryTypeName)
        {
            var settings = new ProgramSettings();
            var item = settings.Items.Add(ProgId, "員工");
            item.Repository = repositoryTypeName ?? string.Empty;
            return settings;
        }

        private static string TypeNameOf<T>() => $"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}";

        private static ProgramSettingsRepositoryTypeResolver CreateResolver(
            ProgramSettings? programs,
            ICustomizeDefineReader? customizeReader = null,
            ISessionInfoService? sessionInfoService = null)
            => new(new StubDefineAccess { Programs = programs }, customizeReader, sessionInfoService);

        #endregion

        [Fact]
        [DisplayName("建構子傳入 null defineAccess 應拋 ArgumentNullException")]
        public void Constructor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ProgramSettingsRepositoryTypeResolver(null!));
        }

        [Fact]
        [DisplayName("Repository 有值應建出註冊的型別")]
        public void Resolve_BoundRepository_ReturnsRegisteredType()
        {
            var resolver = CreateResolver(Registry(TypeNameOf<CustomEmployeeRepository>()));

            var type = resolver.Resolve(Guid.Empty, ProgId);

            Assert.Equal(typeof(CustomEmployeeRepository), type);
        }

        [Fact]
        [DisplayName("Repository 留空應沿用框架預設 DataFormRepository")]
        public void Resolve_EmptyRepository_FallsBackToDefault()
        {
            var resolver = CreateResolver(Registry(repositoryTypeName: null));

            var type = resolver.Resolve(Guid.Empty, ProgId);

            Assert.Equal(typeof(DataFormRepository), type);
        }

        [Fact]
        [DisplayName("註冊表根本沒有這個 progId 時應沿用框架預設，不視為錯誤")]
        public void Resolve_ProgIdNotRegistered_FallsBackToDefault()
        {
            var resolver = CreateResolver(new ProgramSettings());

            var type = resolver.Resolve(Guid.Empty, "Department");

            Assert.Equal(typeof(DataFormRepository), type);
        }

        [Fact]
        [DisplayName("沒有 ProgramSettings.xml 時應沿用框架預設，不視為錯誤")]
        public void Resolve_NoRegistryFile_FallsBackToDefault()
        {
            var resolver = CreateResolver(programs: null);

            var type = resolver.Resolve(Guid.Empty, ProgId);

            Assert.Equal(typeof(DataFormRepository), type);
        }

        [Fact]
        [DisplayName("Repository 型別載不到應直接拋，訊息指名 progId 與型別名")]
        public void Resolve_UnloadableType_ThrowsNamingBoth()
        {
            const string TypeName = "Nowhere.NoSuchRepository, Nowhere.Assembly";
            var resolver = CreateResolver(Registry(TypeName));

            var ex = Assert.Throws<InvalidOperationException>(
                () => resolver.Resolve(Guid.Empty, ProgId));

            Assert.Contains(ProgId, ex.Message, StringComparison.Ordinal);
            Assert.Contains(TypeName, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("Repository 型別非 DataFormRepository 衍生應直接拋，訊息指名 progId 與型別名")]
        public void Resolve_NotDerivedFromDataFormRepository_ThrowsNamingBoth()
        {
            string typeName = TypeNameOf<NotARepository>();
            var resolver = CreateResolver(Registry(typeName));

            var ex = Assert.Throws<InvalidOperationException>(
                () => resolver.Resolve(Guid.Empty, ProgId));

            Assert.Contains(ProgId, ex.Message, StringComparison.Ordinal);
            Assert.Contains(typeName, ex.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(DataFormRepository), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("租戶客製層宣告該 progId 時應整筆取代基底層的綁定")]
        public void Resolve_CustomizationDeclaresProgId_ReplacesBaseBinding()
        {
            var token = Guid.NewGuid();
            var resolver = CreateResolver(
                Registry(TypeNameOf<CustomEmployeeRepository>()),
                new StubCustomizeReader(CustomizeId, Registry(TypeNameOf<TenantEmployeeRepository>())),
                new StubSessionInfoService(token, CustomizeId));

            var type = resolver.Resolve(token, ProgId);

            Assert.Equal(typeof(TenantEmployeeRepository), type);
        }

        [Fact]
        [DisplayName("session 無客製代號時應解析基底層綁定")]
        public void Resolve_SessionWithoutCustomizeId_UsesBaseBinding()
        {
            var token = Guid.NewGuid();
            var resolver = CreateResolver(
                Registry(TypeNameOf<CustomEmployeeRepository>()),
                new StubCustomizeReader(CustomizeId, Registry(TypeNameOf<TenantEmployeeRepository>())),
                new StubSessionInfoService(token, customizeId: string.Empty));

            var type = resolver.Resolve(token, ProgId);

            Assert.Equal(typeof(CustomEmployeeRepository), type);
        }

        [Fact]
        [DisplayName("客製層未宣告該 progId 時應落回基底層綁定")]
        public void Resolve_CustomizationSilentOnProgId_FallsBackToBase()
        {
            var token = Guid.NewGuid();
            var resolver = CreateResolver(
                Registry(TypeNameOf<CustomEmployeeRepository>()),
                new StubCustomizeReader(CustomizeId, new ProgramSettings()),
                new StubSessionInfoService(token, CustomizeId));

            var type = resolver.Resolve(token, ProgId);

            Assert.Equal(typeof(CustomEmployeeRepository), type);
        }
    }
}
