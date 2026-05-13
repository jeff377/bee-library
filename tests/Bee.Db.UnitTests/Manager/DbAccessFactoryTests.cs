using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Db.Manager;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests.Manager
{
    [Collection("DbConnectionState")]
    public class DbAccessFactoryTests : IClassFixture<SharedDbFixture>
    {
        public DbAccessFactoryTests(SharedDbFixture _) { }


        [Fact]
        [DisplayName("DbAccessFactory 預設建構子應建立實例")]
        public void DbAccessFactory_DefaultConstructor_CreatesInstance()
        {
            var factory = new DbAccessFactory();
            Assert.NotNull(factory);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        [InlineData(120)]
        [DisplayName("DbAccessFactory 指定 maxCommandTimeout 應建立實例")]
        public void DbAccessFactory_WithTimeout_CreatesInstance(int timeout)
        {
            var factory = new DbAccessFactory(timeout);
            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("DbAccessFactory.Create 應回傳對應 DatabaseType 的 DbAccess 實例")]
        public void Create_ValidDatabaseId_ReturnsDbAccessWithCorrectType()
        {
            string id = $"bee_factory_{Guid.NewGuid():N}";
            DatabaseSettings settings = BeeTestServices.GetRequiredService<IDefineAccess>().GetDatabaseSettings();
            settings.Items!.Add(new DatabaseItem
            {
                Id = id,
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = "Server=test;"
            });

            try
            {
                var factory = new DbAccessFactory(30);
                var dbAccess = factory.Create(id);

                Assert.NotNull(dbAccess);
                Assert.Equal(DatabaseType.SQLServer, dbAccess.DatabaseType);
            }
            finally
            {
                settings.Items!.Remove(settings.Items[id]!);
                DbConnectionManager.Remove(id);
            }
        }
    }
}
