using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.MySql;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="MySqlTableSchemaProvider"/> 尚未涵蓋的靜態分支與
    /// 需要活 MySQL 連線的 ParseIndexes 路徑（非主鍵索引）。
    /// </summary>
    [Collection("Initialize")]
    public class MySqlTableSchemaProviderEdgeCaseTests
    {
        #region ParseDBDefaultValue 邊緣案例

        [Fact]
        [DisplayName("MySQL ParseDBDefaultValue originalDefault 有開括號但無閉括號時 StripOuterParens 應原樣保留")]
        public void ParseDBDefaultValue_OriginalWithUnclosedParen_NotStripped()
        {
            // StripOuterParens("(CURRENT_TIMESTAMP") → value[last] = 'P' ≠ ')' → 不剝除，回傳原字串
            // "(CURRENT_TIMESTAMP" ≠ "CURRENT_TIMESTAMP" → 回傳 trimmed = "CURRENT_TIMESTAMP"
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue(
                "datetime", "CURRENT_TIMESTAMP", "(CURRENT_TIMESTAMP");
            Assert.Equal("CURRENT_TIMESTAMP", result);
        }

        [Fact]
        [DisplayName("MySQL ParseDBDefaultValue originalDefault 為單字元時 StripOuterParens 長度不足應原樣保留")]
        public void ParseDBDefaultValue_OriginalSingleChar_NotStripped()
        {
            // StripOuterParens("(") → length = 1 < 2 → 不剝除，回傳 "("
            // "(" ≠ "0" → 回傳 trimmed "0"
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue("int", "0", "(");
            Assert.Equal("0", result);
        }

        [Fact]
        [DisplayName("MySQL ParseDBDefaultValue originalDefault 為兩字元 () 時 StripOuterParens 剝除後為空字串")]
        public void ParseDBDefaultValue_OriginalEmptyParens_StrippedToEmpty()
        {
            // StripOuterParens("()") → length=2, starts '(', ends ')' → returns ""
            // "" == "0" is false (case-insensitive) → returns trimmed = "0"
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue("int", "0", "()");
            Assert.Equal("0", result);
        }

        #endregion

        #region 整合測試（需 MySQL 連線）

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider 應正確讀取包含非主鍵索引的資料表結構（覆蓋 ParseIndexes 迴圈主體）")]
        public void SchemaProvider_TableWithNonPkIndex_ParseIndexesLoopBodyCovered()
        {
            const string tableName = "tb_idx_edge";
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var dbAccess = new DbAccess(databaseId);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"DROP TABLE IF EXISTS `{tableName}`"));
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE `{tableName}` (" +
                    "`sys_rowid` CHAR(36) NOT NULL," +
                    "`code` VARCHAR(20) NOT NULL," +
                    "PRIMARY KEY (`sys_rowid`)," +
                    "UNIQUE KEY `IX_tb_idx_edge_code` (`code`)" +
                    ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"));

                var provider = new MySqlTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.True(schema!.Indexes!.Count >= 2, "應有 PK + 至少一個非主鍵唯一索引");
                Assert.True(schema.Indexes.Any(idx => idx.PrimaryKey), "應有主鍵索引");
                Assert.True(schema.Indexes.Any(idx => !idx.PrimaryKey && idx.Unique), "應有非主鍵唯一索引");
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS `{tableName}`"));
            }
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider 讀取無主鍵資料表時 ParsePrimaryKey 應提早返回")]
        public void SchemaProvider_TableWithNoPrimaryKey_ParsePrimaryKeyEarlyReturn()
        {
            const string tableName = "tb_nopk_edge";
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var dbAccess = new DbAccess(databaseId);

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"DROP TABLE IF EXISTS `{tableName}`"));
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE `{tableName}` (" +
                    "`code` VARCHAR(20) NOT NULL" +
                    ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"));

                var provider = new MySqlTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.False(schema!.Indexes!.Any(idx => idx.PrimaryKey), "無主鍵資料表不應有主鍵索引");
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS `{tableName}`"));
            }
        }

        #endregion
    }
}
