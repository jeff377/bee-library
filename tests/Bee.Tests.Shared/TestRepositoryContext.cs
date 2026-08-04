using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Storage;
using Bee.Repository;
using Bee.Repository.Abstractions;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// 建立測試用的 <see cref="IRepositoryContext"/>。
    /// </summary>
    /// <remarks>
    /// Repository 的建構函式統一為 <c>(ctx, accessToken, progId)</c> 後，測試不再逐一注入
    /// 個別服務。多數測試只在意其中一兩個成員，其餘給不會被碰到的 stub 即可。
    /// </remarks>
    public static class TestRepositoryContext
    {
        /// <summary>
        /// 由 fixture 的 DI 容器組出完整 context。
        /// </summary>
        /// <param name="fixture">提供框架服務的 fixture。</param>
        public static IRepositoryContext Create(BeeTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            return new RepositoryContext
            {
                DefineAccess = fixture.GetRequiredService<IDefineAccess>(),
                ConnectionManager = fixture.GetRequiredService<IDbConnectionManager>(),
                DbAccessFactory = fixture.GetRequiredService<IDbAccessFactory>(),
                Router = fixture.GetRequiredService<IRepositoryDatabaseRouter>(),
                CacheNotify = fixture.GetService<ICacheNotifyService>(),
                Services = fixture.Provider,
            };
        }

        /// <summary>
        /// 由呼叫端指定的零件組出 context，未指定者以不會被使用的 stub 補齊。
        /// </summary>
        /// <param name="connectionManager">連線管理員。</param>
        /// <param name="defineAccess">定義存取服務。</param>
        /// <param name="dbAccessFactory">資料庫存取工廠。</param>
        /// <param name="router">資料庫路由。</param>
        /// <param name="cacheNotify">跨行程快取失效通道。</param>
        /// <param name="services">服務提供者。</param>
        public static IRepositoryContext Create(
            IDbConnectionManager? connectionManager = null,
            IDefineAccess? defineAccess = null,
            IDbAccessFactory? dbAccessFactory = null,
            IRepositoryDatabaseRouter? router = null,
            ICacheNotifyService? cacheNotify = null,
            IServiceProvider? services = null)
            => new RepositoryContext
            {
                ConnectionManager = connectionManager!,
                DefineAccess = defineAccess!,
                DbAccessFactory = dbAccessFactory!,
                Router = router ?? new FixedRouter(),
                CacheNotify = cacheNotify,
                Services = services ?? EmptyServiceProvider.Instance,
            };

        /// <summary>
        /// 取得不解析任何服務的 <see cref="IServiceProvider"/>，供只需要滿足簽章的測試使用。
        /// </summary>
        public static IServiceProvider CreateServices() => EmptyServiceProvider.Instance;

        /// <summary>
        /// 預設路由：Common / Log 比照正式 <c>RepositoryDatabaseRouter</c> 回固定 databaseId，
        /// Company 因無 session 可查而回測試用代號。
        /// </summary>
        /// <remarks>
        /// Common / Log 必須與正式路由一致，否則 <c>SessionRepository</c> 之類宣告 Common scope 的
        /// repository 會被導到不存在的資料庫，而測試看到的會是 KeyNotFound 而非它想驗的行為。
        /// </remarks>
        private sealed class FixedRouter : IRepositoryDatabaseRouter
        {
            public string Resolve(DbScope scope, Guid accessToken) => scope switch
            {
                DbScope.Common => DbCategoryIds.Common,
                DbScope.Log => DbCategoryIds.Log,
                _ => "testdb",
            };
        }

        private sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new();
            public object? GetService(Type serviceType) => null;
        }
    }
}
