using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.MySql;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="MySqlTableSchemaProvider"/> 的 DB 路徑覆蓋率：
    /// ParsePrimaryKey 的兩個 early return 分支，以及 ParseDbField 的 Decimal 分支。
    /// </summary>
    [Collection("Initialize")]
    public class MySqlTableSchemaProviderCoverageTests
    {
        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider 無索引表應回傳空 Indexes 集合（觸發 ParsePrimaryKey 第一個 early return）")]
        public void SchemaProvider_TableWithNoIndexes_ReturnsSchemaWithEmptyIndexes()
        {
            const string tableName = "tb_cov_noidx";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var dbAccess = new DbAccess(databaseId);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"DROP TABLE IF EXISTS `{tableName}`"));
            try
            {
                // 無任何索引 → GetTableIndexes 回傳空 DataTable
                // ParsePrimaryKey: table.IsEmpty() = true → 直接 return（第一個 early return）
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE `{tableName}` (`name` VARCHAR(50) NOT NULL) " +
                    "ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"));

                var provider = new MySqlTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Empty(schema!.Indexes!);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS `{tableName}`"));
            }
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider 有 UNIQUE 索引但無主鍵的表應正確解析索引（觸發 ParsePrimaryKey 第二個 early return）")]
        public void SchemaProvider_TableWithUniqueIndexNoPrimaryKey_ParsesUniqueIndex()
        {
            const string tableName = "tb_cov_nopk";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var dbAccess = new DbAccess(databaseId);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"DROP TABLE IF EXISTS `{tableName}`"));
            try
            {
                // UNIQUE INDEX 命名非 'PRIMARY' → IsPrimaryKey 全為 false
                // ParsePrimaryKey: DefaultView.RowFilter="IsPrimaryKey=true" → empty → return（第二個 early return）
                // ParseIndexes: while 迴圈讀取唯一索引
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE `{tableName}` (" +
                    "  `name` VARCHAR(50) NOT NULL, " +
                    "  UNIQUE KEY `ux_cov_nopk` (`name`)" +
                    ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"));

                var provider = new MySqlTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Empty(schema!.Indexes!.Where(i => i.PrimaryKey));
                Assert.Single(schema.Indexes!.Where(i => !i.PrimaryKey && i.Unique));
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS `{tableName}`"));
            }
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider Decimal 欄位應正確解析 Precision 和 Scale（觸發 ParseDbField Decimal 分支）")]
        public void SchemaProvider_TableWithDecimalColumn_ParsesPrecisionAndScale()
        {
            const string tableName = "tb_cov_decimal";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var dbAccess = new DbAccess(databaseId);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"DROP TABLE IF EXISTS `{tableName}`"));
            try
            {
                // DECIMAL(12,3) → GetFieldDbType 回傳 Decimal → ParseDbField 進入 Decimal 分支
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE `{tableName}` (" +
                    "  `id` BIGINT NOT NULL AUTO_INCREMENT, " +
                    "  `amount` DECIMAL(12,3) NOT NULL, " +
                    "  PRIMARY KEY (`id`)" +
                    ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"));

                var provider = new MySqlTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var amountField = schema!.Fields!["amount"];
                Assert.NotNull(amountField);
                Assert.Equal(FieldDbType.Decimal, amountField.DbType);
                Assert.Equal(12, amountField.Precision);
                Assert.Equal(3, amountField.Scale);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS `{tableName}`"));
            }
        }
    }
}
