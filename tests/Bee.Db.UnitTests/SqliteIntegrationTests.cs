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

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite SchemaProvider 應讀回非主鍵的二級索引（含 unique 旗標）")]
        public void SchemaProvider_ReadsSecondaryIndexes()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            var dbAccess = new Bee.Db.DbAccess(databaseId);

            // 直接以最小 DDL 建立帶二級索引的表，驗證 ParseIndexes / ReadIndexFields 路徑。
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "DROP TABLE IF EXISTS idx_test"));
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "CREATE TABLE idx_test (id INTEGER PRIMARY KEY, name VARCHAR(50) NOT NULL, code VARCHAR(20) NOT NULL)"));
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "CREATE INDEX ix_idx_test_name ON idx_test (name)"));
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "CREATE UNIQUE INDEX uk_idx_test_code ON idx_test (code)"));

            try
            {
                var provider = new Bee.Db.Providers.Sqlite.SqliteTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema("idx_test");

                Assert.NotNull(schema);
                // 預期：pk_idx_test（內部 rename 之 PK） + ix_idx_test_name + uk_idx_test_code
                var nonPk = schema!.Indexes!.Where(i => !i.PrimaryKey).ToList();
                Assert.Equal(2, nonPk.Count);

                var nameIdx = nonPk.Single(i => i.Name == "ix_idx_test_name");
                Assert.False(nameIdx.Unique);
                Assert.Equal("name", nameIdx.IndexFields![0].FieldName);

                var codeIdx = nonPk.Single(i => i.Name == "uk_idx_test_code");
                Assert.True(codeIdx.Unique);
                Assert.Equal("code", codeIdx.IndexFields![0].FieldName);
            }
            finally
            {
                dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                    "DROP TABLE IF EXISTS idx_test"));
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite SchemaProvider 應解析 NUMERIC(precision,scale) 並還原 Decimal 欄位的精度")]
        public void SchemaProvider_ReadsDecimalPrecisionAndScale()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            var dbAccess = new Bee.Db.DbAccess(databaseId);

            // NUMERIC(12,3) 觸發 ParseTypeFacets 的多參數分支與 Decimal precision/scale 還原。
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "DROP TABLE IF EXISTS dec_test"));
            dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                "CREATE TABLE dec_test (id INTEGER PRIMARY KEY, amount NUMERIC(12,3) NOT NULL)"));

            try
            {
                var provider = new Bee.Db.Providers.Sqlite.SqliteTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema("dec_test");

                Assert.NotNull(schema);
                var amount = schema!.Fields!["amount"];
                Assert.Equal(FieldDbType.Decimal, amount.DbType);
                Assert.Equal(12, amount.Precision);
                Assert.Equal(3, amount.Scale);
            }
            finally
            {
                dbAccess.Execute(new Bee.Db.DbCommandSpec(Bee.Db.DbCommandKind.NonQuery,
                    "DROP TABLE IF EXISTS dec_test"));
            }
        }

    }
}
