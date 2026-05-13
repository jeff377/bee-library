using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.MySql;
using Bee.Definition.Database;
using Bee.Tests.Shared;
using Bee.Db.Manager;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="MySqlTableSchemaProvider"/> 整合路徑：
    /// 驗證 <c>ParseDbField</c> Decimal 分支（Precision/Scale 賦值）與
    /// <c>ParsePrimaryKey</c> 在無主鍵時的提前返回路徑。
    /// 依賴 MySQL 連線；環境變數未設時自動跳過。
    /// </summary>
    public class MySqlTableSchemaProviderExtraTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public MySqlTableSchemaProviderExtraTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider 讀回 DECIMAL(15,3) 欄位時應正確設定 Precision 與 Scale（ParseDbField Decimal 分支）")]
        public void GetTableSchema_DecimalField_ReturnsPrecisionAndScale()
        {
            const string tableName = "tb_ex_decimal";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var dbAccess = _fx.NewDbAccess(databaseId);
            DropMySqlTable(dbAccess, tableName);

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE tb_ex_decimal (id VARCHAR(36) NOT NULL, " +
                    "amount DECIMAL(15,3) NOT NULL, " +
                    "CONSTRAINT PK_TB_EX_DECIMAL PRIMARY KEY (id)) " +
                    "ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"));

                var provider = new MySqlTableSchemaProvider(databaseId, _fx.GetRequiredService<IDbConnectionManager>());
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
                DropMySqlTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL SchemaProvider 讀取只有唯一索引（無 PK）的資料表時 ParsePrimaryKey 應提前返回且唯一索引仍正確解析")]
        public void GetTableSchema_TableWithUniqueIndexNoPk_ParsePrimaryKeyReturnsEarlyAndIndexPresent()
        {
            const string tableName = "tb_ex_nopk";
            string databaseId = TestDbConventions.GetDatabaseId(DatabaseType.MySQL);
            var dbAccess = _fx.NewDbAccess(databaseId);
            DropMySqlTable(dbAccess, tableName);

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE TABLE tb_ex_nopk (code VARCHAR(20) NOT NULL) " +
                    "ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "CREATE UNIQUE INDEX UX_TB_EX_NOPK ON tb_ex_nopk (code)"));

                var provider = new MySqlTableSchemaProvider(databaseId, _fx.GetRequiredService<IDbConnectionManager>());
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.True(schema!.Fields!.Contains("code"));
                Assert.NotEmpty(schema.Indexes!);
                Assert.DoesNotContain(schema.Indexes!, idx => idx.PrimaryKey);
            }
            finally
            {
                DropMySqlTable(dbAccess, tableName);
            }
        }

        private static void DropMySqlTable(DbAccess dbAccess, string tableName)
        {
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "DROP TABLE IF EXISTS " + tableName));
        }
    }
}
