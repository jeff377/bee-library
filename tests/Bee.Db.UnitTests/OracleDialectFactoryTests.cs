using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Manager;
using Bee.Db.Providers.Oracle;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Oracle provider/dialect smoke tests. Verifies that the test fixture
    /// (<see cref="Bee.Tests.Shared.GlobalFixture"/>) registers both the ADO.NET
    /// provider factory and the dialect factory at startup, so subsequent Oracle
    /// builder/integration tests can resolve them via the registries.
    /// </summary>
    /// <remarks>
    /// Phase B-1: only the factory wiring is validated here; the actual builder
    /// implementations are stubs that throw <see cref="NotImplementedException"/>
    /// and will be filled in by follow-up commits — see
    /// docs/plans/plan-oracle-support.md.
    /// </remarks>
    [Collection("Initialize")]
    public class OracleDialectFactoryTests
    {
        [Fact]
        [DisplayName("Oracle DialectFactory 應透過 DbDialectRegistry 註冊成功")]
        public void DialectFactory_IsRegistered()
        {
            var factory = DbDialectRegistry.Get(DatabaseType.Oracle);

            Assert.NotNull(factory);
            Assert.IsType<OracleDialectFactory>(factory);
        }

        [Fact]
        [DisplayName("Oracle ADO.NET provider 應透過 DbProviderRegistry 註冊成功")]
        public void Provider_IsRegistered()
        {
            var factory = DbProviderRegistry.Get(DatabaseType.Oracle);

            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("Oracle DbProviderRegistry 應註冊 connection-open initializer")]
        public void Provider_ConnectionInitializer_IsRegistered()
        {
            var initializer = DbProviderRegistry.GetConnectionInitializer(DatabaseType.Oracle);

            // GlobalFixture.RegisterOracle 掛上 ALTER SESSION 動作；只驗證 hook 存在，
            // 真正執行的測試在 BEE_TEST_CONNSTR_ORACLE 啟用後由整合測試涵蓋。
            Assert.NotNull(initializer);
        }

        [Fact]
        [DisplayName("Oracle DialectFactory 的 GetDefaultValueExpression 應回傳對應運算式")]
        public void DialectFactory_DefaultValueExpression_ReturnsExpected()
        {
            var factory = new OracleDialectFactory();

            Assert.Equal("SYS_GUID()", factory.GetDefaultValueExpression(FieldDbType.Guid));
            Assert.Equal("SYSTIMESTAMP", factory.GetDefaultValueExpression(FieldDbType.DateTime));
            Assert.Equal("0", factory.GetDefaultValueExpression(FieldDbType.Integer));
            Assert.Equal(string.Empty, factory.GetDefaultValueExpression(FieldDbType.String));
        }

        [Fact]
        [DisplayName("Oracle DialectFactory 應能建立各純 SQL 產生器實例")]
        public void DialectFactory_CreatesAllBuilders()
        {
            var factory = new OracleDialectFactory();

            // 只驗證「無外部相依」的 builder：CREATE / ALTER / Rebuild 純字串輸出，
            // 不需開連線或查 FormSchema。
            Assert.NotNull(factory.CreateCreateTableCommandBuilder());
            Assert.NotNull(factory.CreateTableAlterCommandBuilder());
            Assert.NotNull(factory.CreateTableRebuildCommandBuilder());
            // CreateTableSchemaProvider / CreateFormCommandBuilder 與 MySQL 同樣依賴
            // databaseId / FormSchema，由整合與 builder 測試覆蓋（plan Phase C-2 / B-3）。
        }
    }
}
