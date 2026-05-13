using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="SqlTableSchemaProvider"/> NormalizeDataTypeName 邊界路徑：
    /// null/empty 輸入。
    /// </summary>
    public class SqlTableSchemaProviderNullTests
    {
        [Fact]
        [DisplayName("SQL Server GetFieldDbType null 輸入應回傳 Unknown")]
        public void GetFieldDbType_NullDataType_ReturnsUnknown()
        {
            Assert.Equal(FieldDbType.Unknown, SqlTableSchemaProvider.GetFieldDbType(null!, 0, 0, 0));
        }

        [Fact]
        [DisplayName("SQL Server GetFieldDbType 空字串輸入應回傳 Unknown")]
        public void GetFieldDbType_EmptyDataType_ReturnsUnknown()
        {
            Assert.Equal(FieldDbType.Unknown, SqlTableSchemaProvider.GetFieldDbType(string.Empty, 0, 0, 0));
        }
    }

    /// <summary>
    /// <see cref="SqlTableSchemaProvider"/> 整合測試：
    /// 驗證 <c>ParseDbField</c> Decimal 分支（Precision/Scale 賦值）與
    /// <c>ParsePrimaryKey</c> 在無主鍵時的提前返回路徑。
    /// 依賴 SQL Server 連線；環境變數未設時自動跳過。
    /// </summary>
    public class SqlTableSchemaProviderDecimalAndNoPkTests : IClassFixture<SharedDbFixture>
    {
        public SqlTableSchemaProviderDecimalAndNoPkTests(SharedDbFixture _) { }


        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider 讀回 DECIMAL(15,3) 欄位時應正確設定 Precision 與 Scale（ParseDbField Decimal 分支）")]
        public void GetTableSchema_DecimalField_ReturnsPrecisionAndScale()
        {
            const string tableName = "tb_ex_decimal";
            var dbAccess = new DbAccess("common_sqlserver");
            DropSqlTable(dbAccess, tableName);

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE [tb_ex_decimal] " +
                    "([id] UNIQUEIDENTIFIER NOT NULL, [amount] DECIMAL(15,3) NOT NULL, " +
                    "CONSTRAINT [PK_TB_EX_DECIMAL] PRIMARY KEY ([id]))"));

                var provider = new SqlTableSchemaProvider("common_sqlserver");
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
                DropSqlTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider 讀取只有唯一索引（無 PK）的資料表時 ParsePrimaryKey 應提前返回且唯一索引仍正確解析")]
        public void GetTableSchema_TableWithUniqueIndexNoPk_ParsePrimaryKeyReturnsEarlyAndIndexPresent()
        {
            const string tableName = "tb_ex_nopk";
            var dbAccess = new DbAccess("common_sqlserver");
            DropSqlTable(dbAccess, tableName);

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE [tb_ex_nopk] ([code] NVARCHAR(20) NOT NULL)"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE UNIQUE INDEX [UX_TB_EX_NOPK] ON [tb_ex_nopk] ([code])"));

                var provider = new SqlTableSchemaProvider("common_sqlserver");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.True(schema!.Fields!.Contains("code"));
                Assert.NotEmpty(schema.Indexes!);
                Assert.DoesNotContain(schema.Indexes!, idx => idx.PrimaryKey);
            }
            finally
            {
                DropSqlTable(dbAccess, tableName);
            }
        }

        private static void DropSqlTable(DbAccess dbAccess, string tableName)
        {
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "IF OBJECT_ID(N'[" + tableName + "]', N'U') IS NOT NULL DROP TABLE [" + tableName + "]"));
        }
    }
}
