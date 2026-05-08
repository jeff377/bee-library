using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="OracleTableSchemaProvider"/> 尚未涵蓋的靜態分支與
    /// 需要活 Oracle 連線的 ParseIndexes / ParsePrimaryKey 路徑。
    /// </summary>
    [Collection("Initialize")]
    public class OracleTableSchemaProviderEdgeCaseTests
    {
        #region GetFieldDbType 邊緣案例

        [Fact]
        [DisplayName("Oracle GetFieldDbType null 輸入應回傳 Unknown")]
        public void GetFieldDbType_NullInput_ReturnsUnknown()
        {
            Assert.Equal(FieldDbType.Unknown, OracleTableSchemaProvider.GetFieldDbType(null!, 0, 0, 0));
        }

        [Fact]
        [DisplayName("Oracle GetFieldDbType 型別名稱有開括號但無閉括號時 NormalizeDataTypeName 應原樣保留並回傳 Unknown")]
        public void GetFieldDbType_MalformedTypeWithUnclosedParen_ReturnsUnknown()
        {
            // NormalizeDataTypeName: parenStart=9, parenEnd=-1 → if (parenEnd < 0) return lower
            // "timestamp(6" 不符合 switch 任何 case → Unknown
            Assert.Equal(FieldDbType.Unknown, OracleTableSchemaProvider.GetFieldDbType("TIMESTAMP(6", 0, 0, 0));
        }

        [Fact]
        [DisplayName("Oracle GetFieldDbType TIMESTAMP WITH TIME ZONE 應回傳 Unknown（括號後剩餘文字保留）")]
        public void GetFieldDbType_TimestampWithTimeZone_ReturnsUnknown()
        {
            // NormalizeDataTypeName("TIMESTAMP(6) WITH TIME ZONE"):
            // before="timestamp", after=" with time zone" → "timestamp with time zone"
            // 不符合 "timestamp" case → Unknown
            Assert.Equal(FieldDbType.Unknown, OracleTableSchemaProvider.GetFieldDbType("TIMESTAMP(6) WITH TIME ZONE", 0, 0, 0));
        }

        #endregion

        #region ParseDBDefaultValue 邊緣案例

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue LONG 型別應走 default 分支並回傳 trimmed 值")]
        public void ParseDBDefaultValue_LongType_ReturnsTrimmedValue()
        {
            // "LONG" → NormalizeDataTypeName → "long"
            // "long" 不在字串型別 switch（僅含 varchar2/nvarchar2/char/nchar/clob/nclob）→ default: normalized = trimmed
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("LONG", "  my_default  ", "");
            Assert.Equal("my_default", result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue BLOB 型別應走 default 分支並回傳 trimmed 值")]
        public void ParseDBDefaultValue_BlobType_ReturnsTrimmedValue()
        {
            // "BLOB" 不在字串型別 switch → default: normalized = trimmed
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("BLOB", "  hexvalue  ", "");
            Assert.Equal("hexvalue", result);
        }

        #endregion

        #region 整合測試（需 Oracle 連線）

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle SchemaProvider 應正確讀取包含非主鍵索引的資料表結構（覆蓋 ParseIndexes 迴圈主體）")]
        public void SchemaProvider_TableWithNonPkIndex_ParseIndexesLoopBodyCovered()
        {
            const string tableName = "tb_idx_edge";
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.Oracle);
            var dbAccess = new DbAccess(databaseId);

            DropTable(dbAccess, tableName);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE \"TB_IDX_EDGE\" (" +
                    "\"SYS_ROWID\" RAW(16) NOT NULL," +
                    "\"CODE\" VARCHAR2(20 CHAR) NOT NULL," +
                    "CONSTRAINT \"PK_TB_IDX_EDGE\" PRIMARY KEY (\"SYS_ROWID\")" +
                    ")"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE UNIQUE INDEX \"IX_TB_IDX_EDGE_CODE\" ON \"TB_IDX_EDGE\" (\"CODE\")"));

                var provider = new OracleTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.True(schema!.Indexes!.Count >= 2, "應有 PK + 至少一個非主鍵唯一索引");
                Assert.True(schema.Indexes.Any(idx => idx.PrimaryKey), "應有主鍵索引");
                Assert.True(schema.Indexes.Any(idx => !idx.PrimaryKey && idx.Unique), "應有非主鍵唯一索引");
            }
            finally
            {
                DropTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle SchemaProvider 讀取無主鍵資料表時 ParsePrimaryKey 應提早返回")]
        public void SchemaProvider_TableWithNoPrimaryKey_ParsePrimaryKeyEarlyReturn()
        {
            const string tableName = "tb_nopk_edge";
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.Oracle);
            var dbAccess = new DbAccess(databaseId);

            DropTable(dbAccess, tableName);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE \"TB_NOPK_EDGE\" (" +
                    "\"CODE\" VARCHAR2(20 CHAR) NOT NULL" +
                    ")"));

                var provider = new OracleTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.False(schema!.Indexes!.Any(idx => idx.PrimaryKey), "無主鍵資料表不應有主鍵索引");
            }
            finally
            {
                DropTable(dbAccess, tableName);
            }
        }

        private static void DropTable(DbAccess dbAccess, string tableName)
        {
            string storageName = tableName.ToUpperInvariant();
            string ddl =
                "BEGIN " +
                "  EXECUTE IMMEDIATE 'DROP TABLE \"" + storageName + "\" CASCADE CONSTRAINTS'; " +
                "EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; " +
                "END;";
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, ddl));
        }

        #endregion
    }
}
