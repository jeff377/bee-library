using System.ComponentModel;
using System.Globalization;
using Bee.Base.Data;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// SQLite-specific integration tests that exercise the schema provider, schema upgrade
    /// orchestrator and provider-level constraints against a live SQLite database. The
    /// FormMap IUD round-trip is covered separately in
    /// <see cref="FormCommandBuilderIudIntegrationTests"/>.
    /// </summary>
    [Collection("Initialize")]
    public class SqliteIntegrationTests
    {
        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite SchemaProvider 應讀回 fixture 建好的 st_user 表")]
        public void SchemaProvider_ReadsFixtureTable()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            var provider = new Bee.Db.Providers.Sqlite.SqliteTableSchemaProvider(databaseId);

            var schema = provider.GetTableSchema("st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName);
            // sys_no is INTEGER PRIMARY KEY AUTOINCREMENT — read back as AutoIncrement.
            Assert.True(schema.Fields!.Contains("sys_no"));
            Assert.Equal(FieldDbType.AutoIncrement, schema.Fields["sys_no"].DbType);
            Assert.True(schema.Fields.Contains("sys_rowid"));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite SchemaProvider 應對不存在的表回傳 null")]
        public void SchemaProvider_UnknownTable_ReturnsNull()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            var provider = new Bee.Db.Providers.Sqlite.SqliteTableSchemaProvider(databaseId);

            var schema = provider.GetTableSchema("__no_such_table__");

            Assert.Null(schema);
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite text 欄位字串比對應為 case-insensitive（COLLATE NOCASE）")]
        public void StringComparison_IsCaseInsensitive()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            var dbAccess = new Bee.Db.DbAccess(databaseId);

            // 手寫 minimal DDL 以聚焦於驗證 SQLite 對 COLLATE NOCASE 欄位的執行行為，
            // 與 SqliteCreateTableCommandBuilder 純語法測試獨立。
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "DROP TABLE IF EXISTS ci_test"));
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "CREATE TABLE ci_test (name VARCHAR(50) COLLATE NOCASE NOT NULL)"));

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
