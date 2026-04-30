using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Manager;
using Bee.Db.Providers.PostgreSql;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// PostgreSQL provider/dialect smoke tests. Verifies that the test fixture
    /// (<see cref="Bee.Tests.Shared.GlobalFixture"/>) registers both the ADO.NET
    /// provider factory and the dialect factory at startup, so subsequent PG
    /// builder/integration tests can resolve them via the registries.
    /// </summary>
    [Collection("Initialize")]
    public class PgDialectFactoryTests
    {
        [Fact]
        [DisplayName("PG DialectFactory 應透過 DbDialectRegistry 註冊成功")]
        public void DialectFactory_IsRegistered()
        {
            var factory = DbDialectRegistry.Get(DatabaseType.PostgreSQL);

            Assert.NotNull(factory);
            Assert.IsType<PgDialectFactory>(factory);
        }

        [Fact]
        [DisplayName("PG ADO.NET provider 應透過 DbProviderRegistry 註冊成功")]
        public void Provider_IsRegistered()
        {
            var factory = DbProviderRegistry.Get(DatabaseType.PostgreSQL);

            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("PG DialectFactory 的 GetDefaultValueExpression 應回傳對應運算式")]
        public void DialectFactory_DefaultValueExpression_ReturnsExpected()
        {
            var factory = new PgDialectFactory();

            Assert.Equal("gen_random_uuid()", factory.GetDefaultValueExpression(FieldDbType.Guid));
            Assert.Equal("CURRENT_TIMESTAMP", factory.GetDefaultValueExpression(FieldDbType.DateTime));
            Assert.Equal("CURRENT_TIMESTAMP", factory.GetDefaultValueExpression(FieldDbType.Date));
            Assert.Equal("0", factory.GetDefaultValueExpression(FieldDbType.Integer));
            Assert.Equal("0", factory.GetDefaultValueExpression(FieldDbType.Boolean));
            Assert.Equal(string.Empty, factory.GetDefaultValueExpression(FieldDbType.String));
            Assert.Equal(string.Empty, factory.GetDefaultValueExpression(FieldDbType.Text));
        }

        [Fact]
        [DisplayName("PG DialectFactory 應能建立各純 SQL 產生器實例")]
        public void DialectFactory_CreatesAllBuilders()
        {
            var factory = new PgDialectFactory();

            // 只驗證「無外部相依」的 builder：CREATE / ALTER / Rebuild 純字串輸出，
            // 不需開連線或查 FormSchema。
            Assert.NotNull(factory.CreateCreateTableCommandBuilder());
            Assert.NotNull(factory.CreateTableAlterCommandBuilder());
            Assert.NotNull(factory.CreateTableRebuildCommandBuilder());
            // CreateTableSchemaProvider / CreateFormCommandBuilder 倚賴 databaseId / FormSchema，
            // 由整合與 builder 測試覆蓋。
        }
    }
}
