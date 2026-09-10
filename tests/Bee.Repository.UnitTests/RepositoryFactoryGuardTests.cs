using System.ComponentModel;
using System.Data.Common;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
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
    /// <see cref="RepositoryFactory"/> 建構子的相依防護、progId 軸解析定義錯誤時的失敗語意，
    /// 以及工廠與 <see cref="IRepositoryTypeResolver"/> 之間的委派契約。
    /// 以 stub 相依隔離，不需要資料庫；兩軸的正常解析路徑見 <see cref="RepositoryFactoryTests"/>，
    /// 註冊表綁定本身的解析見 <see cref="ProgramSettingsRepositoryTypeResolverTests"/>。
    /// </summary>
    public class RepositoryFactoryGuardTests
    {
        #region Stubs

        private sealed class StubDefineAccess : IDefineAccess
        {
            public string CategoryId { get; set; } = DbCategoryIds.Common;

            public DatabaseSettings GetDatabaseSettings() => new();
            public FormSchema GetFormSchema(string progId) => new() { CategoryId = CategoryId };
            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotImplementedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotImplementedException();
            public SystemSettings GetSystemSettings() => throw new NotImplementedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotImplementedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotImplementedException();
            // 本檔不驗註冊表綁定，一律回報「沒有 ProgramSettings.xml」——這是正式
            // IDefineAccess 在檔案不存在時的行為，工廠據此落回框架預設 repository。
            // 綁定本身的解析見 ProgramSettingsRepositoryTypeResolverTests。
            public ProgramSettings GetProgramSettings() => throw new FileNotFoundException("ProgramSettings.xml");
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

        /// <summary>固定回傳指定型別，並記下工廠傳進來的引數。</summary>
        private sealed class StubTypeResolver(Type type) : IRepositoryTypeResolver
        {
            public Guid? ReceivedAccessToken { get; private set; }
            public string? ReceivedProgId { get; private set; }

            public Type Resolve(Guid accessToken, string progId)
            {
                ReceivedAccessToken = accessToken;
                ReceivedProgId = progId;
                return type;
            }
        }

        /// <summary>resolver 綁定的自訂 repository。</summary>
        public class BoundRepository : DataFormRepository
        {
            public BoundRepository(IRepositoryContext ctx, Guid accessToken, string progId)
                : base(ctx, accessToken, progId)
            {
            }
        }

        /// <summary>不衍生自 <see cref="DataFormRepository"/>，用來驗證工廠端的契約檢查。</summary>
        public class NotARepository
        {
        }

        private static RepositoryFactory CreateFactory(
            StubDefineAccess? defineAccess = null,
            IDbAccessFactory? dbAccessFactory = null,
            IDbConnectionManager? connectionManager = null,
            IRepositoryDatabaseRouter? router = null,
            IRepositoryTypeResolver? typeResolver = null)
        {
            var define = defineAccess ?? new StubDefineAccess();
            return new(
                TestRepositoryContext.CreateServices(),
                define,
                dbAccessFactory ?? new StubDbAccessFactory(),
                connectionManager ?? new StubConnectionManager(),
                router ?? new StubRouter(),
                typeResolver ?? new ProgramSettingsRepositoryTypeResolver(define));
        }

        private static StubTypeResolver DefaultResolver() => new(typeof(DataFormRepository));

        #endregion

        [Fact]
        [DisplayName("RepositoryFactory 建構子傳入 null defineAccess 應拋 ArgumentNullException")]
        public void RepositoryFactory_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RepositoryFactory(
                TestRepositoryContext.CreateServices(), null!, new StubDbAccessFactory(),
                new StubConnectionManager(), new StubRouter(), DefaultResolver()));
        }

        [Fact]
        [DisplayName("RepositoryFactory 建構子傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void RepositoryFactory_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RepositoryFactory(
                TestRepositoryContext.CreateServices(), new StubDefineAccess(), null!,
                new StubConnectionManager(), new StubRouter(), DefaultResolver()));
        }

        [Fact]
        [DisplayName("RepositoryFactory 建構子傳入 null connectionManager 應拋 ArgumentNullException")]
        public void RepositoryFactory_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RepositoryFactory(
                TestRepositoryContext.CreateServices(), new StubDefineAccess(), new StubDbAccessFactory(),
                null!, new StubRouter(), DefaultResolver()));
        }

        [Fact]
        [DisplayName("RepositoryFactory 建構子傳入 null router 應拋 ArgumentNullException")]
        public void RepositoryFactory_NullRouter_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RepositoryFactory(
                TestRepositoryContext.CreateServices(), new StubDefineAccess(), new StubDbAccessFactory(),
                new StubConnectionManager(), null!, DefaultResolver()));
        }

        [Fact]
        [DisplayName("RepositoryFactory 建構子傳入 null typeResolver 應拋 ArgumentNullException")]
        public void RepositoryFactory_NullTypeResolver_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RepositoryFactory(
                TestRepositoryContext.CreateServices(), new StubDefineAccess(), new StubDbAccessFactory(),
                new StubConnectionManager(), new StubRouter(), null!));
        }

        [Fact]
        [DisplayName("工廠應依 resolver 給的型別建出實例，並帶上 progId")]
        public void CreateFormRepository_ResolverBindsCustomType_BuildsThatTypeWithProgId()
        {
            var factory = CreateFactory(typeResolver: new StubTypeResolver(typeof(BoundRepository)));

            var repository = factory.CreateFormRepository<IDataFormRepository>(Guid.NewGuid(), "Employee");

            var typed = Assert.IsType<BoundRepository>(repository);
            Assert.Equal("Employee", typed.ProgId);
        }

        [Fact]
        [DisplayName("工廠應把呼叫端的 accessToken 與 progId 原樣交給 resolver（租戶客製靠 token 找 session）")]
        public void CreateFormRepository_ForwardsAccessTokenAndProgIdToResolver()
        {
            // token 若沒傳到 resolver，客製代號就讀不到、租戶的 Repository 覆寫整批失效，
            // 而每個請求照樣成功 —— 預設路徑的測試全都看不出來。
            var resolver = DefaultResolver();
            var factory = CreateFactory(typeResolver: resolver);
            var token = Guid.NewGuid();

            factory.CreateFormRepository<IDataFormRepository>(token, "Employee");

            Assert.Equal(token, resolver.ReceivedAccessToken);
            Assert.Equal("Employee", resolver.ReceivedProgId);
        }

        [Fact]
        [DisplayName("resolver 回傳非 DataFormRepository 衍生型別時，工廠應拋並指名 progId 與型別")]
        public void CreateFormRepository_ResolverReturnsNonRepositoryType_ThrowsNamingProgIdAndType()
        {
            var factory = CreateFactory(typeResolver: new StubTypeResolver(typeof(NotARepository)));

            var ex = Assert.Throws<InvalidOperationException>(
                () => factory.CreateFormRepository<IDataFormRepository>(Guid.NewGuid(), "Employee"));

            Assert.Contains("Employee", ex.Message, StringComparison.Ordinal);
            Assert.Contains(typeof(NotARepository).FullName!, ex.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(DataFormRepository), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("CreateFormRepository 傳入空白 progId 應拋 ArgumentException")]
        public void CreateFormRepository_WhitespaceProgId_ThrowsArgumentException()
        {
            var factory = CreateFactory();
            Assert.Throws<ArgumentException>(() => factory.CreateFormRepository<IDataFormRepository>(Guid.NewGuid(), "   "));
        }

        [Fact]
        [DisplayName("CreateFormRepository Schema 無 CategoryId 應拋 InvalidOperationException 且訊息含 CategoryId")]
        public void CreateFormRepository_EmptyCategoryId_ThrowsInvalidOperationException()
        {
            var stub = new StubDefineAccess { CategoryId = string.Empty };
            var factory = CreateFactory(defineAccess: stub);
            var ex = Assert.Throws<InvalidOperationException>(
                () => factory.CreateFormRepository<IDataFormRepository>(Guid.NewGuid(), "Employee"));
            Assert.Contains("CategoryId", ex.Message);
        }

        [Fact]
        [DisplayName("CreateFormRepository 未知 CategoryId 應拋 InvalidOperationException 且訊息含未知值")]
        public void CreateFormRepository_UnknownCategoryId_ThrowsInvalidOperationException()
        {
            var stub = new StubDefineAccess { CategoryId = "unknown_db" };
            var factory = CreateFactory(defineAccess: stub);
            var ex = Assert.Throws<InvalidOperationException>(
                () => factory.CreateFormRepository<IDataFormRepository>(Guid.NewGuid(), "Employee"));
            Assert.Contains("unknown_db", ex.Message);
        }

        [Theory]
        [InlineData(DbCategoryIds.Common)]
        [InlineData(DbCategoryIds.Company)]
        [InlineData(DbCategoryIds.Log)]
        [DisplayName("CreateFormRepository 有效 CategoryId 應回傳 DataFormRepository")]
        public void CreateFormRepository_ValidCategoryId_ReturnsDataFormRepository(string categoryId)
        {
            var stub = new StubDefineAccess { CategoryId = categoryId };
            var factory = CreateFactory(defineAccess: stub);
            var repo = factory.CreateFormRepository<IDataFormRepository>(Guid.NewGuid(), "Employee");
            Assert.IsType<DataFormRepository>(repo);
        }
    }
}
