using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="OracleTableSchemaProvider"/> NormalizeDataTypeName 邊界路徑：
    /// null/empty 輸入、括號後有後綴文字、只有左括號無右括號。
    /// </summary>
    public class OracleTableSchemaProviderNullTests
    {
        [Fact]
        [DisplayName("Oracle GetFieldDbType null 輸入應回傳 Unknown")]
        public void GetFieldDbType_NullDataType_ReturnsUnknown()
        {
            Assert.Equal(FieldDbType.Unknown, OracleTableSchemaProvider.GetFieldDbType(null!, 0, 0, 0));
        }

        [Fact]
        [DisplayName("Oracle GetFieldDbType 空字串輸入應回傳 Unknown")]
        public void GetFieldDbType_EmptyDataType_ReturnsUnknown()
        {
            Assert.Equal(FieldDbType.Unknown, OracleTableSchemaProvider.GetFieldDbType(string.Empty, 0, 0, 0));
        }

        [Fact]
        [DisplayName("Oracle GetFieldDbType TIMESTAMP(6) WITH TIME ZONE 應回傳 Unknown（NormalizeDataTypeName 括號後有後綴文字的 after 分支）")]
        public void GetFieldDbType_TimestampWithTimeZoneQualifier_ReturnsUnknown()
        {
            // NormalizeDataTypeName("TIMESTAMP(6) WITH TIME ZONE"):
            // lower="timestamp(6) with time zone", parenStart=9, parenEnd=11
            // before="timestamp", after=" with time zone" → result="timestamp with time zone"
            // → 不在 switch → Unknown
            Assert.Equal(FieldDbType.Unknown,
                OracleTableSchemaProvider.GetFieldDbType("TIMESTAMP(6) WITH TIME ZONE", 0, 0, 0));
        }

        [Fact]
        [DisplayName("Oracle GetFieldDbType 只有左括號無右括號時應回傳 Unknown（NormalizeDataTypeName parenEnd<0 分支）")]
        public void GetFieldDbType_TypeWithOpenParenNoCloseParen_ReturnsUnknown()
        {
            // NormalizeDataTypeName("FLOAT("):
            // lower="float(", parenStart=5, parenEnd=lower.IndexOf(')', 5)=-1
            // → if (parenEnd < 0) return lower; → returns "float(" → switch default → Unknown
            Assert.Equal(FieldDbType.Unknown,
                OracleTableSchemaProvider.GetFieldDbType("FLOAT(", 0, 0, 0));
        }
    }

    /// <summary>
    /// <see cref="OracleTableSchemaProvider"/> 整合測試：
    /// 驗證 <c>ParseDbField</c> Decimal 分支（Precision/Scale 賦值）與
    /// <c>ParsePrimaryKey</c> 在無主鍵時的提前返回路徑。
    /// 依賴 Oracle 連線；環境變數未設時自動跳過。
    /// </summary>
    public class OracleTableSchemaProviderDecimalAndNoPkTests : IClassFixture<SharedDbFixture>
    {
        public OracleTableSchemaProviderDecimalAndNoPkTests(SharedDbFixture _) { }


        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle SchemaProvider 讀回 NUMBER(15,3) 欄位時應正確設定 Precision 與 Scale（ParseDbField Decimal 分支）")]
        public void GetTableSchema_DecimalField_ReturnsPrecisionAndScale()
        {
            const string tableName = "tb_ex_decimal";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.Oracle);
            var dbAccess = new DbAccess(databaseId);
            DropOracleTable(dbAccess, tableName);

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE \"TB_EX_DECIMAL\" " +
                    "(\"id\" RAW(16) NOT NULL, \"amount\" NUMBER(15,3) NOT NULL, " +
                    "CONSTRAINT \"PK_TB_EX_DECIMAL\" PRIMARY KEY (\"id\"))"));

                var provider = new OracleTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.True(schema!.Fields!.Contains("amount"));
                var field = schema.Fields["amount"];
                Assert.Equal(FieldDbType.Decimal, field.DbType);
                Assert.Equal(15, field.Precision);
                Assert.Equal(3, field.Scale);
            }
            finally
            {
                DropOracleTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle SchemaProvider 讀取只有唯一索引（無 PK）的資料表時 ParsePrimaryKey 應提前返回且唯一索引仍正確解析")]
        public void GetTableSchema_TableWithUniqueIndexNoPk_ParsePrimaryKeyReturnsEarlyAndIndexPresent()
        {
            const string tableName = "tb_ex_nopk";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.Oracle);
            var dbAccess = new DbAccess(databaseId);
            DropOracleTable(dbAccess, tableName);

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE \"TB_EX_NOPK\" (\"code\" VARCHAR2(20 CHAR) NOT NULL)"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE UNIQUE INDEX \"UX_TB_EX_NOPK\" ON \"TB_EX_NOPK\" (\"code\")"));

                var provider = new OracleTableSchemaProvider(databaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.True(schema!.Fields!.Contains("code"));
                Assert.NotEmpty(schema.Indexes!);
                Assert.DoesNotContain(schema.Indexes!, idx => idx.PrimaryKey);
            }
            finally
            {
                DropOracleTable(dbAccess, tableName);
            }
        }

        private static void DropOracleTable(DbAccess dbAccess, string tableName)
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
