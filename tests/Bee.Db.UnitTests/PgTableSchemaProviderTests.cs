using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.PostgreSql;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class PgTableSchemaProviderTests
    {
        private const string DatabaseId = "common_postgresql";

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PgTableSchemaProvider 取得資料表結構應成功")]
        public void GetTableSchema_ValidTableName_ReturnsSchema()
        {
            var helper = new PgTableSchemaProvider(DatabaseId);
            var dbTable = helper.GetTableSchema("st_user");
            Assert.NotNull(dbTable);
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PgTableSchemaProvider 取得不存在資料表應回傳 null")]
        public void GetTableSchema_NonExistentTable_ReturnsNull()
        {
            var helper = new PgTableSchemaProvider(DatabaseId);
            var dbTable = helper.GetTableSchema("bee_nonexistent_table_xyz_99999");
            Assert.Null(dbTable);
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PgTableSchemaProvider DatabaseId 應等於建構子傳入的值")]
        public void Constructor_DatabaseId_IsSet()
        {
            var helper = new PgTableSchemaProvider(DatabaseId);
            Assert.Equal(DatabaseId, helper.DatabaseId);
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetTableSchema 應從 COMMENT ON TABLE 讀回表層 DisplayName")]
        public void GetTableSchema_WithComment_ReturnsDisplayName()
        {
            string tableName = $"bee_test_desc_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE \"{tableName}\" (\"id\" integer NOT NULL);"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"COMMENT ON TABLE \"{tableName}\" IS '測試表說明';"));

                var provider = new PgTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Equal("測試表說明", schema!.DisplayName);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS \"{tableName}\";"));
            }
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetTableSchema 無 COMMENT 時 DisplayName 應為空字串")]
        public void GetTableSchema_WithoutComment_ReturnsEmptyDisplayName()
        {
            string tableName = $"bee_test_desc_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE \"{tableName}\" (\"id\" integer NOT NULL);"));

                var provider = new PgTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Equal(string.Empty, schema!.DisplayName);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS \"{tableName}\";"));
            }
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetTableSchema 應正確解析 Decimal 欄位的 Precision 與 Scale")]
        public void GetTableSchema_DecimalColumn_ParsesPrecisionAndScale()
        {
            string tableName = $"bee_test_decimal_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE \"{tableName}\" (\"id\" integer NOT NULL, \"amount\" numeric(12,3) NOT NULL);"));

                var provider = new PgTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var amount = schema!.Fields![@"amount"];
                Assert.Equal(FieldDbType.Decimal, amount.DbType);
                Assert.Equal(12, amount.Precision);
                Assert.Equal(3, amount.Scale);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS \"{tableName}\";"));
            }
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetTableSchema 應同時解析主鍵與非主鍵索引")]
        public void GetTableSchema_PrimaryKeyAndSecondaryIndex_BothParsed()
        {
            string tableName = $"bee_test_idx_{Guid.NewGuid():N}";
            string idxName = $"ix_{tableName}_name";
            var dbAccess = new DbAccess(DatabaseId);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE \"{tableName}\" (\"id\" integer NOT NULL, \"name\" varchar(50) NOT NULL, " +
                    $"CONSTRAINT \"pk_{tableName}\" PRIMARY KEY (\"id\"));"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE INDEX \"{idxName}\" ON \"{tableName}\" (\"name\");"));

                var provider = new PgTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                // Primary key index
                var pk = schema!.GetPrimaryKey();
                Assert.NotNull(pk);
                Assert.True(pk!.PrimaryKey);
                Assert.Single(pk.IndexFields!);
                Assert.Equal("id", pk.IndexFields![0].FieldName);

                // Secondary (non-primary) index
                var secondary = schema.Indexes!.Cast<TableSchemaIndex>().FirstOrDefault(i => !i.PrimaryKey);
                Assert.NotNull(secondary);
                Assert.Equal(idxName, secondary!.Name);
                Assert.Single(secondary.IndexFields!);
                Assert.Equal("name", secondary.IndexFields![0].FieldName);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS \"{tableName}\";"));
            }
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetTableSchema 無主鍵的資料表應回傳空 PK，欄位仍可解析")]
        public void GetTableSchema_NoPrimaryKey_ReturnsSchemaWithoutPk()
        {
            string tableName = $"bee_test_nopk_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE \"{tableName}\" (\"value\" integer NOT NULL);"));

                var provider = new PgTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Null(schema!.GetPrimaryKey());
                Assert.NotNull(schema.Fields!["value"]);
                Assert.Equal(FieldDbType.Integer, schema.Fields!["value"].DbType);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS \"{tableName}\";"));
            }
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetTableSchema AutoIncrement 欄位應解析為 FieldDbType.AutoIncrement")]
        public void GetTableSchema_AutoIncrementColumn_MapsToAutoIncrement()
        {
            string tableName = $"bee_test_identity_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE \"{tableName}\" (\"id\" integer GENERATED BY DEFAULT AS IDENTITY NOT NULL);"));

                var provider = new PgTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Equal(FieldDbType.AutoIncrement, schema!.Fields!["id"].DbType);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS \"{tableName}\";"));
            }
        }
    }
}
