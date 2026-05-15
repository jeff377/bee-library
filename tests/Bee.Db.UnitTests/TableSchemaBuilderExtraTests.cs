using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    public class TableSchemaBuilderExtraTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public TableSchemaBuilderExtraTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("TableSchemaBuilder 建構子傳入空白 databaseId 應拋出 ArgumentException")]
        public void Ctor_WhitespaceDatabaseId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TableSchemaBuilder("   ", null!, null!));
        }

        [Fact]
        [DisplayName("TableSchemaBuilder 建構子傳入 null defineAccess 應拋出 ArgumentNullException")]
        public void Ctor_NullDefineAccess_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TableSchemaBuilder(
                    "test_db",
                    null!,
                    _fx.GetRequiredService<IDbConnectionManager>()));
        }

        [Fact]
        [DisplayName("TableSchemaBuilder 建構子傳入 null connectionManager 應拋出 ArgumentNullException")]
        public void Ctor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TableSchemaBuilder(
                    "test_db",
                    _fx.GetRequiredService<IDefineAccess>(),
                    null!));
        }
    }
}
