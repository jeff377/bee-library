using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="OracleTableSchemaProvider"/> 覆蓋率：
    /// 1. 靜態方法 NormalizeDataTypeName 的邊緣分支（括號但無閉括號、括號後有額外修飾詞）。
    /// 2. ParsePrimaryKey / ParseDbField 的 DB 路徑（無索引表、無 PK 表、Decimal 欄位）。
    /// </summary>
    [Collection("Initialize")]
    public class OracleTableSchemaProviderCoverageTests
    {
        // ─── 靜態方法邊緣案例（不需資料庫）───────────────────────────────────

        [Fact]
        [DisplayName("Oracle GetFieldDbType 帶後置修飾詞的型別（如 TIMESTAMP(6) WITH TIME ZONE）應回傳 Unknown")]
        public void GetFieldDbType_TimestampWithTimeZoneQualifier_ReturnsUnknown()
        {
            // NormalizeDataTypeName("TIMESTAMP(6) WITH TIME ZONE")
            //   → "timestamp(6) with time zone"
            //   → before="timestamp", after=" with time zone" → "timestamp with time zone"
            // switch 無此 case → Unknown
            var result = OracleTableSchemaProvider.GetFieldDbType("TIMESTAMP(6) WITH TIME ZONE", 0, 0, 0);

            Assert.Equal(FieldDbType.Unknown, result);
        }

        [Fact]
        [DisplayName("Oracle GetFieldDbType 有開括號但無閉括號的型別應回傳 Unknown")]
        public void GetFieldDbType_TypeWithOpenParenNoClose_ReturnsUnknown()
        {
            // NormalizeDataTypeName("VARCHAR2(")
            //   → "varchar2("
            //   → parenStart=8, parenEnd=-1 → parenEnd < 0 → return "varchar2("
            // switch 無此 case → Unknown
            var result = OracleTableSchemaProvider.GetFieldDbType("VARCHAR2(", 0, 0, 0);

            Assert.Equal(FieldDbType.Unknown, result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue 帶後置修飾詞的型別不剝除引號，直接 trim")]
        public void ParseDBDefaultValue_TimestampWithTimeZone_ReturnsNonQuotedValue()
        {
            // dataType "TIMESTAMP(6) WITH TIME ZONE" → NormalizeDataTypeName → "timestamp with time zone"
            // switch default → normalized = trimmed → no stripping
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("TIMESTAMP(6) WITH TIME ZONE", "SYSTIMESTAMP", "");

            Assert.Equal("SYSTIMESTAMP", result);
        }

        // ─── Oracle 整合測試：需要 Oracle 連線 ────────────────────────────────

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle SchemaProvider 無索引表應回傳空 Indexes 集合（觸發 ParsePrimaryKey 第一個 early return）")]
        public void SchemaProvider_TableWithNoIndexes_ReturnSchemaWithEmptyIndexes()
        {
            const string tableName = "tb_cov_noidx";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.Oracle);
            var dbAccess = new DbAccess(databaseId);
            DropQuotedTable(dbAccess, tableName);

            try
            {
                // 建立無任何索引的表 — GetTableIndexes 回傳空 DataTable
                // → ParsePrimaryKey: table.IsEmpty() = true → 直接 return（第一個 early return）
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE \"TB_COV_NOIDX\" (\"name\" VARCHAR2(50 CHAR) NOT NULL)"));

                var provider = new OracleTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Empty(schema!.Indexes!);
            }
            finally
            {
                DropQuotedTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle SchemaProvider 有 UNIQUE 索引但無主鍵的表應正確解析索引（觸發 ParsePrimaryKey 第二個 early return）")]
        public void SchemaProvider_TableWithUniqueIndexNoPrimaryKey_ParsesUniqueIndex()
        {
            const string tableName = "tb_cov_nopk";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.Oracle);
            var dbAccess = new DbAccess(databaseId);
            DropQuotedTable(dbAccess, tableName);

            try
            {
                // 有 UNIQUE 索引但無 PK 約束 → IsPrimaryKey 全為 0
                // ParsePrimaryKey: table.DefaultView.RowFilter="IsPrimaryKey=true" → empty → return（第二個 early return）
                // ParseIndexes: while 迴圈讀取唯一索引
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE \"TB_COV_NOPK\" (\"name\" VARCHAR2(50 CHAR) NOT NULL)"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE UNIQUE INDEX \"UX_COV_NOPK\" ON \"TB_COV_NOPK\" (\"name\")"));

                var provider = new OracleTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Empty(schema!.Indexes!.Where(i => i.PrimaryKey));
                Assert.Single(schema.Indexes!.Where(i => !i.PrimaryKey && i.Unique));
            }
            finally
            {
                DropQuotedTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle SchemaProvider Decimal 欄位應正確解析 Precision 和 Scale（觸發 ParseDbField Decimal 分支）")]
        public void SchemaProvider_TableWithDecimalColumn_ParsesPrecisionAndScale()
        {
            const string tableName = "tb_cov_decimal";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.Oracle);
            var dbAccess = new DbAccess(databaseId);
            DropQuotedTable(dbAccess, tableName);

            try
            {
                // NUMBER(12,3) → GetFieldDbType 回傳 Decimal → ParseDbField 進入 Decimal 分支
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE \"TB_COV_DECIMAL\" (" +
                    "  \"id\" NUMBER(10) NOT NULL, " +
                    "  \"amount\" NUMBER(12,3) NOT NULL, " +
                    "  CONSTRAINT \"PK_COV_DECIMAL\" PRIMARY KEY (\"id\"))"));

                var provider = new OracleTableSchemaProvider(databaseId);
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
                DropQuotedTable(dbAccess, tableName);
            }
        }

        private static void DropQuotedTable(DbAccess dbAccess, string tableName)
        {
            string storageName = tableName.ToUpperInvariant();
            string ddl =
                "BEGIN " +
                "  EXECUTE IMMEDIATE 'DROP TABLE \"" + storageName + "\" CASCADE CONSTRAINTS'; " +
                "EXCEPTION WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE; END IF; " +
                "END;";
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, ddl));
        }
    }
}
