using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Manager;
using Bee.Db.Providers.MySql;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// MySQL provider/dialect smoke tests. Verifies that the test fixture
    /// (<see cref="Bee.Tests.Shared.GlobalFixture"/>) registers both the ADO.NET
    /// provider factory and the dialect factory at startup, so subsequent MySQL
    /// builder/integration tests can resolve them via the registries.
    /// </summary>
    /// <remarks>
    /// Phase A: only the factory wiring is validated here; the actual builder
    /// implementations are stubs that throw <see cref="NotImplementedException"/>
    /// and will be filled in by follow-up commits — see
    /// docs/plans/plan-mysql-support.md.
    /// </remarks>
    public class MySqlDialectFactoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public MySqlDialectFactoryTests(SharedDbFixture fx) { _fx = fx; }
        [Fact]
        [DisplayName("MySQL DialectFactory 應透過 DbDialectRegistry 註冊成功")]
        public void DialectFactory_IsRegistered()
        {
            var factory = DbDialectRegistry.Get(DatabaseType.MySQL);

            Assert.NotNull(factory);
            Assert.IsType<MySqlDialectFactory>(factory);
        }

        [Fact]
        [DisplayName("MySQL ADO.NET provider 應透過 DbProviderRegistry 註冊成功")]
        public void Provider_IsRegistered()
        {
            var factory = DbProviderRegistry.Get(DatabaseType.MySQL);

            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("MySQL DialectFactory 的 GetDefaultValueExpression 應回傳對應運算式")]
        public void DialectFactory_DefaultValueExpression_ReturnsExpected()
        {
            var factory = new MySqlDialectFactory();

            Assert.Equal("(UUID())", factory.GetDefaultValueExpression(FieldDbType.Guid));
            Assert.Equal("CURRENT_TIMESTAMP(6)", factory.GetDefaultValueExpression(FieldDbType.DateTime));
            Assert.Equal("0", factory.GetDefaultValueExpression(FieldDbType.Integer));
            Assert.Equal(string.Empty, factory.GetDefaultValueExpression(FieldDbType.String));
        }

        [Fact]
        [DisplayName("MySQL DialectFactory 應能建立各純 SQL 產生器實例")]
        public void DialectFactory_CreatesAllBuilders()
        {
            var factory = new MySqlDialectFactory();

            // 只驗證「無外部相依」的 builder：CREATE / ALTER / Rebuild 純字串輸出，
            // 不需開連線或查 FormSchema。
            Assert.NotNull(factory.CreateCreateTableCommandBuilder());
            Assert.NotNull(factory.CreateTableAlterCommandBuilder());
            Assert.NotNull(factory.CreateTableRebuildCommandBuilder());
            // CreateTableSchemaProvider 在實作後會 ctor 內 new DbAccess(databaseId)，
            // CI 未設 BEE_TEST_CONNSTR_MYSQL 時 'common_mysql' 沒註冊到 DbConnectionManager
            // → KeyNotFoundException；改由 MySqlIntegrationTests 覆蓋。
        }

        [Fact]
        [DisplayName("MySqlDialectFactory：CreateFormCommandBuilder 應回傳 MySqlFormCommandBuilder")]
        public void CreateFormCommandBuilder_ReturnsMySqlImpl()
        {
            var factory = new MySqlDialectFactory();
            var schema = new FormSchema("Foo", "Foo");
            var defineAccess = _fx.GetRequiredService<IDefineAccess>();

            var builder = factory.CreateFormCommandBuilder(schema, defineAccess);

            Assert.IsType<MySqlFormCommandBuilder>(builder);
        }
    }
}
