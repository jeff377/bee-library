using System.ComponentModel;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Factories;
using Bee.Repository.Form;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="RepositoryFactory"/> 的兩個軸：progId 軸（型別隨 progId 變動）與框架軸
    /// （型別固定、以介面指名）。
    /// </summary>
    public class RepositoryFactoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public RepositoryFactoryTests(SharedDbFixture fx) { _fx = fx; }

        private RepositoryFactory CreateFactory()
            => new(
                _fx.Provider,
                _fx.GetRequiredService<Definition.Storage.IDefineAccess>(),
                _fx.GetRequiredService<Db.IDbAccessFactory>(),
                _fx.GetRequiredService<Db.Manager.IDbConnectionManager>(),
                new StubRouter());

        /// <summary>
        /// 沿用正式路由對 Common / Log 的固定規則，Company 直接回測試代號。
        /// </summary>
        /// <remarks>
        /// tests/Define 下每張 FormSchema 都是 company scope，正式路由需要一個已進入公司的
        /// session 才解析得出來。本檔要驗的是工廠本身（型別對應、契約檢查、scope 宣告），
        /// session 路由另有 RepositoryDatabaseRouter 的測試涵蓋。
        /// </remarks>
        private sealed class StubRouter : IRepositoryDatabaseRouter
        {
            public string Resolve(Definition.DbScope scope, Guid accessToken) => scope switch
            {
                Definition.DbScope.Common => Definition.Database.DbCategoryIds.Common,
                Definition.DbScope.Log => Definition.Database.DbCategoryIds.Log,
                _ => "testdb",
            };
        }

        // ---- 框架軸 ----

        [Theory]
        [InlineData(typeof(ISessionRepository))]
        [InlineData(typeof(ICompanyRepository))]
        [InlineData(typeof(IUserCompanyRepository))]
        [InlineData(typeof(IUserRepository))]
        [InlineData(typeof(IApiKeyRepository))]
        [InlineData(typeof(IDatabaseRepository))]
        [InlineData(typeof(IRolePermissionRepository))]
        [InlineData(typeof(IDepartmentRepository))]
        [InlineData(typeof(IEmployeeRepository))]
        [InlineData(typeof(IAuditLogRepository))]
        [InlineData(typeof(IAuditLogWriteRepository))]
        [DisplayName("Create<T> 應能建立每一個框架 repository")]
        public void Create_EveryFrameworkRepository_Resolves(Type contract)
        {
            var factory = CreateFactory();
            var method = typeof(RepositoryFactory).GetMethod(nameof(RepositoryFactory.Create))!
                .MakeGenericMethod(contract);

            var repository = method.Invoke(factory, [Guid.Empty]);

            Assert.NotNull(repository);
            Assert.IsType(contract, repository, exactMatch: false);
        }

        [Fact]
        [DisplayName("Create<T> 未註冊的介面應拋 NotSupportedException 並指名該介面")]
        public void Create_UnregisteredContract_Throws()
        {
            var factory = CreateFactory();

            var ex = Assert.Throws<NotSupportedException>(() => factory.Create<IDisposable>());

            Assert.Contains(nameof(IDisposable), ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("每次 Create<T> 應回傳新實例——repository 帶 per-call 狀態，不可共用")]
        public void Create_ReturnsNewInstanceEachTime()
        {
            var factory = CreateFactory();

            Assert.NotSame(factory.Create<ISessionRepository>(), factory.Create<ISessionRepository>());
        }

        [Fact]
        [DisplayName("宣告 Common scope 的 repository 應解析到 common 資料庫")]
        public void Create_CommonScopeRepository_RoutesToCommon()
        {
            var repository = (RepositoryBase)CreateFactory().Create<ISessionRepository>();

            Assert.Equal(Definition.Database.DbCategoryIds.Common, GetDatabaseId(repository));
        }

        [Fact]
        [DisplayName("每個方法自帶 databaseId 的 repository 不應在建構期解析路由")]
        public void Create_CallerRoutedRepository_ResolvesNoDatabase()
        {
            // 這三個的呼叫端是 cache provider 與 session bootstrap：它們被告知要讀哪一家公司，
            // 手上沒有 token。若建構期就以 session 解析，讀到的會是呼叫者的公司而不是被指定的那家。
            foreach (var repository in new RepositoryBase[]
            {
                (RepositoryBase)CreateFactory().Create<IRolePermissionRepository>(),
                (RepositoryBase)CreateFactory().Create<IDepartmentRepository>(),
                (RepositoryBase)CreateFactory().Create<IEmployeeRepository>(),
            })
            {
                Assert.Equal(string.Empty, GetDatabaseId(repository));
            }
        }

        [Fact]
        [DisplayName("無 session 時建立 Common scope repository 不應拋例外")]
        public void Create_CommonScopeWithoutSession_DoesNotThrow()
        {
            var factory = CreateFactory();

            var exception = Record.Exception(() => factory.Create<IUserRepository>(Guid.Empty));

            Assert.Null(exception);
        }

        // ---- progId 軸 ----

        [Fact]
        [DisplayName("CreateFormRepository 應回傳綁定該 progId 的 repository")]
        public void CreateFormRepository_ReturnsRepositoryBoundToProgId()
        {
            var repository = CreateFactory()
                .CreateFormRepository<IDataFormRepository>(Guid.Empty, "Employee");

            var typed = Assert.IsType<DataFormRepository>(repository);
            Assert.Equal("Employee", typed.ProgId);
        }

        [Fact]
        [DisplayName("CreateFormRepository 要求的介面若解析型別未實作應拋出並指名兩者")]
        public void CreateFormRepository_UnimplementedContract_Throws()
        {
            var factory = CreateFactory();

            var ex = Assert.Throws<InvalidOperationException>(
                () => factory.CreateFormRepository<IUnimplementedFormRepository>(Guid.Empty, "Employee"));

            Assert.Contains("Employee", ex.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(IUnimplementedFormRepository), ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [DisplayName("CreateFormRepository 傳入空白 progId 應拋 ArgumentException")]
        public void CreateFormRepository_BlankProgId_Throws(string? progId)
        {
            var factory = CreateFactory();

            Assert.ThrowsAny<ArgumentException>(
                () => factory.CreateFormRepository<IDataFormRepository>(Guid.Empty, progId!));
        }

        /// <summary>沒有任何 repository 實作的介面，用於驗證型別不符時的錯誤訊息。</summary>
        public interface IUnimplementedFormRepository : IDataFormRepository { }

        private static string GetDatabaseId(RepositoryBase repository)
            => (string)typeof(RepositoryBase)
                .GetProperty("DatabaseId", global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance)!
                .GetValue(repository)!;
    }
}
