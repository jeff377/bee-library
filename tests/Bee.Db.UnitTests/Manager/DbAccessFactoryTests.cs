using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Settings;

namespace Bee.Db.UnitTests.Manager
{
    /// <summary>
    /// DbAccessFactory 的建構與型別解析測試。
    /// </summary>
    /// <remarks>
    /// 自帶隔離的 <see cref="DatabaseSettings"/>，不碰 process-wide 的定義快取 ——
    /// 理由見 <see cref="IsolatedDatabaseSettingsProvider"/>。這裡不開連線，
    /// 只驗工廠回傳的 <c>DbAccess</c> 帶對 <c>DatabaseType</c>。
    /// </remarks>
    public sealed class DbAccessFactoryTests : IDisposable
    {
        private readonly IsolatedDatabaseSettingsProvider _provider = new();
        private readonly DbConnectionManagerService _manager;

        public DbAccessFactoryTests()
        {
            TestDbProviders.EnsureSqlServerRegistered();
            _manager = new DbConnectionManagerService(_provider);
        }

        public void Dispose() => _manager.Dispose();

        [Fact]
        [DisplayName("DbAccessFactory 構造子需要 IDbConnectionManager")]
        public void DbAccessFactory_NullManager_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DbAccessFactory(null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        [InlineData(120)]
        [DisplayName("DbAccessFactory 指定 maxCommandTimeout 應建立實例")]
        public void DbAccessFactory_WithTimeout_CreatesInstance(int timeout)
        {
            var factory = new DbAccessFactory(_manager, timeout);
            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("DbAccessFactory.Create 應回傳對應 DatabaseType 的 DbAccess 實例")]
        public void Create_ValidDatabaseId_ReturnsDbAccessWithCorrectType()
        {
            string id = $"bee_factory_{Guid.NewGuid():N}";
            _provider.Settings.Items!.Add(new DatabaseItem
            {
                Id = id,
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=test;"
            });

            var factory = new DbAccessFactory(_manager, 30);
            var dbAccess = factory.Create(id);

            Assert.NotNull(dbAccess);
            Assert.Equal(DatabaseType.SQLServer, dbAccess.DatabaseType);
        }
    }
}
