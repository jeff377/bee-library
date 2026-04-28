using System.ComponentModel;
using System.Globalization;
using Bee.Base.Data;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// MySQL-specific integration tests: schema reader round-trip against the live fixture
    /// table, and engine-level case-insensitive comparison provided by the day-1 collation
    /// <c>utf8mb4_0900_ai_ci</c>. Skipped when no MySQL connection string is configured
    /// (<see cref="DbFactAttribute"/> handles the env-var check).
    /// </summary>
    [Collection("Initialize")]
    public class MySqlIntegrationTests
    {
        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider 應讀回 fixture 建好的 st_user 表")]
        public void SchemaProvider_ReadsFixtureTable()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var provider = new Bee.Db.Providers.MySql.MySqlTableSchemaProvider(databaseId);

            var schema = provider.GetTableSchema("st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName);
            // sys_no is BIGINT AUTO_INCREMENT PRIMARY KEY — read back as AutoIncrement.
            Assert.True(schema.Fields!.Contains("sys_no"));
            Assert.Equal(FieldDbType.AutoIncrement, schema.Fields["sys_no"].DbType);
            Assert.True(schema.Fields.Contains("sys_rowid"));
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider 應對不存在的表回傳 null")]
        public void SchemaProvider_UnknownTable_ReturnsNull()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var provider = new Bee.Db.Providers.MySql.MySqlTableSchemaProvider(databaseId);

            var schema = provider.GetTableSchema("__no_such_table__");

            Assert.Null(schema);
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL 文字欄位字串比對應為 case-insensitive（utf8mb4_0900_ai_ci）")]
        public void StringComparison_IsCaseInsensitive()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var dbAccess = new Bee.Db.DbAccess(databaseId);

            // 手寫 minimal DDL 以聚焦於驗證 MySQL 對 utf8mb4_0900_ai_ci collation 的執行行為，
            // 與 MySqlCreateTableCommandBuilder 純語法測試獨立。
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "DROP TABLE IF EXISTS ci_test"));
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "CREATE TABLE ci_test (name VARCHAR(50) NOT NULL) " +
                "ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci"));

            try
            {
                dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                    "INSERT INTO ci_test (name) VALUES ({0})", "Jeff"));

                var result = dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM ci_test WHERE name = {0}", "jeff"));

                Assert.NotNull(result);
                Assert.Equal(1, Convert.ToInt32(result.Scalar!, CultureInfo.InvariantCulture));
            }
            finally
            {
                dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                    "DROP TABLE IF EXISTS ci_test"));
            }
        }
    }
}
