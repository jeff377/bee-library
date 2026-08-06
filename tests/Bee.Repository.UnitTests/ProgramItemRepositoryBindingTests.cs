using System.ComponentModel;
using System.Data.Common;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Factories;
using Bee.Repository.Form;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <c>ProgramItem.Repository</c> 的四條解析路徑（有值 / 無值 / 型別載不到 / 型別非衍生）
    /// 與租戶客製 overlay 的取代行為。
    /// </summary>
    /// <remarks>
    /// 全以 stub 相依隔離，不需要資料庫：本檔驗的是「註冊表說什麼 → 工廠建出什麼」，
    /// 建出來的 repository 有沒有連上 DB 不在範圍內。
    /// </remarks>
    public class ProgramItemRepositoryBindingTests
    {
        private const string ProgId = "Employee";
        private const string CustomizeId = "acme";

        #region Stubs 與測試用 repository

        /// <summary>綁定成功時應建出的自訂 repository。</summary>
        public class CustomEmployeeRepository : DataFormRepository
        {
            public CustomEmployeeRepository(IRepositoryContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId)
            {
            }
        }

        /// <summary>租戶客製層應建出的 repository，用來證明客製項整筆取代基底項。</summary>
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

            public FormSchema GetFormSchema(string progId) => new() { CategoryId = DbCategoryIds.Common };

            public ProgramSettings GetProgramSettings()
                => Programs ?? throw new FileNotFoundException("ProgramSettings.xml");

            public DatabaseSettings GetDatabaseSettings() => new();
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

        private sealed class StubDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotImplementedException();
        }

        private sealed class StubConnectionManager : IDbConnectionManager
        {
            public DbConnectionInfo GetConnectionInfo(string databaseId) => throw new NotImplementedException();
            public DbConnection CreateConnection(string databaseId) => throw new NotImplementedException();
            public bool Remove(string databaseId) => false;
            public void Clear() { }
            public bool Contains(string databaseId) => false;
            public int Count => 0;
        }

        private sealed class StubRouter : IRepositoryDatabaseRouter
        {
            public string Resolve(DbScope scope, Guid accessToken) => DbCategoryIds.Common;
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

        private static RepositoryFactory CreateFactory(
            ProgramSettings? programs,
            ICustomizeDefineReader? customizeReader = null,
            ISessionInfoService? sessionInfoService = null)
            => new(
                TestRepositoryContext.CreateServices(),
                new StubDefineAccess { Programs = programs },
                new StubDbAccessFactory(),
                new StubConnectionManager(),
                new StubRouter(),
                cacheNotify: null,
                customizeReader: customizeReader,
                sessionInfoService: sessionInfoService);

        #endregion

        [Fact]
        [DisplayName("Repository 有值應建出註冊的型別")]
        public void CreateFormRepository_BoundRepository_ReturnsRegisteredType()
        {
            var factory = CreateFactory(Registry(TypeNameOf<CustomEmployeeRepository>()));

            var repository = factory.CreateFormRepository<IDataFormRepository>(Guid.Empty, ProgId);

            var typed = Assert.IsType<CustomEmployeeRepository>(repository);
            Assert.Equal(ProgId, typed.ProgId);
        }

        [Fact]
        [DisplayName("Repository 留空應沿用框架預設 DataFormRepository")]
        public void CreateFormRepository_EmptyRepository_FallsBackToDefault()
        {
            var factory = CreateFactory(Registry(repositoryTypeName: null));

            var repository = factory.CreateFormRepository<IDataFormRepository>(Guid.Empty, ProgId);

            Assert.IsType<DataFormRepository>(repository);
        }

        [Fact]
        [DisplayName("註冊表根本沒有這個 progId 時應沿用框架預設，不視為錯誤")]
        public void CreateFormRepository_ProgIdNotRegistered_FallsBackToDefault()
        {
            var factory = CreateFactory(new ProgramSettings());

            var repository = factory.CreateFormRepository<IDataFormRepository>(Guid.Empty, "Department");

            Assert.IsType<DataFormRepository>(repository);
        }

        [Fact]
        [DisplayName("沒有 ProgramSettings.xml 時應沿用框架預設，不視為錯誤")]
        public void CreateFormRepository_NoRegistryFile_FallsBackToDefault()
        {
            var factory = CreateFactory(programs: null);

            var repository = factory.CreateFormRepository<IDataFormRepository>(Guid.Empty, ProgId);

            Assert.IsType<DataFormRepository>(repository);
        }

        [Fact]
        [DisplayName("Repository 型別載不到應直接拋，訊息指名 progId 與型別名")]
        public void CreateFormRepository_UnloadableType_ThrowsNamingBoth()
        {
            const string TypeName = "Nowhere.NoSuchRepository, Nowhere.Assembly";
            var factory = CreateFactory(Registry(TypeName));

            var ex = Assert.Throws<InvalidOperationException>(
                () => factory.CreateFormRepository<IDataFormRepository>(Guid.Empty, ProgId));

            Assert.Contains(ProgId, ex.Message, StringComparison.Ordinal);
            Assert.Contains(TypeName, ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("Repository 型別非 DataFormRepository 衍生應直接拋，訊息指名 progId 與型別名")]
        public void CreateFormRepository_NotDerivedFromDataFormRepository_ThrowsNamingBoth()
        {
            string typeName = TypeNameOf<NotARepository>();
            var factory = CreateFactory(Registry(typeName));

            var ex = Assert.Throws<InvalidOperationException>(
                () => factory.CreateFormRepository<IDataFormRepository>(Guid.Empty, ProgId));

            Assert.Contains(ProgId, ex.Message, StringComparison.Ordinal);
            Assert.Contains(typeName, ex.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(DataFormRepository), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("租戶客製層宣告該 progId 時應整筆取代基底層的綁定")]
        public void CreateFormRepository_CustomizationDeclaresProgId_ReplacesBaseBinding()
        {
            var token = Guid.NewGuid();
            var factory = CreateFactory(
                Registry(TypeNameOf<CustomEmployeeRepository>()),
                new StubCustomizeReader(CustomizeId, Registry(TypeNameOf<TenantEmployeeRepository>())),
                new StubSessionInfoService(token, CustomizeId));

            var repository = factory.CreateFormRepository<IDataFormRepository>(token, ProgId);

            Assert.IsType<TenantEmployeeRepository>(repository);
        }

        [Fact]
        [DisplayName("session 無客製代號時應解析基底層綁定")]
        public void CreateFormRepository_SessionWithoutCustomizeId_UsesBaseBinding()
        {
            var token = Guid.NewGuid();
            var factory = CreateFactory(
                Registry(TypeNameOf<CustomEmployeeRepository>()),
                new StubCustomizeReader(CustomizeId, Registry(TypeNameOf<TenantEmployeeRepository>())),
                new StubSessionInfoService(token, customizeId: string.Empty));

            var repository = factory.CreateFormRepository<IDataFormRepository>(token, ProgId);

            Assert.IsType<CustomEmployeeRepository>(repository);
        }

        [Fact]
        [DisplayName("客製層未宣告該 progId 時應落回基底層綁定")]
        public void CreateFormRepository_CustomizationSilentOnProgId_FallsBackToBase()
        {
            var token = Guid.NewGuid();
            var factory = CreateFactory(
                Registry(TypeNameOf<CustomEmployeeRepository>()),
                new StubCustomizeReader(CustomizeId, new ProgramSettings()),
                new StubSessionInfoService(token, CustomizeId));

            var repository = factory.CreateFormRepository<IDataFormRepository>(token, ProgId);

            Assert.IsType<CustomEmployeeRepository>(repository);
        }
    }
}
