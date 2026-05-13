using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests.Manager
{
    public class DbAccessFactoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public DbAccessFactoryTests(SharedDbFixture fx) { _fx = fx; }

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
            var factory = new DbAccessFactory(_fx.GetRequiredService<IDbConnectionManager>(), timeout);
            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("DbAccessFactory.Create 應回傳對應 DatabaseType 的 DbAccess 實例")]
        public void Create_ValidDatabaseId_ReturnsDbAccessWithCorrectType()
        {
            string id = $"bee_factory_{Guid.NewGuid():N}";
            DatabaseSettings settings = _fx.GetRequiredService<IDefineAccess>().GetDatabaseSettings();
            settings.Items!.Add(new DatabaseItem
            {
                Id = id,
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=test;"
            });
            var manager = _fx.GetRequiredService<IDbConnectionManager>();

            try
            {
                var factory = new DbAccessFactory(manager, 30);
                var dbAccess = factory.Create(id);

                Assert.NotNull(dbAccess);
                Assert.Equal(DatabaseType.SQLServer, dbAccess.DatabaseType);
            }
            finally
            {
                settings.Items!.Remove(settings.Items[id]!);
                manager.Remove(id);
            }
        }
    }
}
