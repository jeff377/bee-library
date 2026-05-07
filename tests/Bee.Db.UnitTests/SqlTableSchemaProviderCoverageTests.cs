using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="SqlTableSchemaProvider"/> 的 DB 路徑覆蓋率：
    /// ParsePrimaryKey 的兩個 early return 分支、ParseDbField 的 NCHAR else 分支
    /// 以及 Decimal 分支。
    /// </summary>
    [Collection("Initialize")]
    public class SqlTableSchemaProviderCoverageTests
    {
        private const string DatabaseId = "common_sqlserver";

        private static void DropTable(DbAccess dbAccess, string tableName)
        {
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"IF (SELECT COUNT(*) FROM sys.tables WHERE name=N'{tableName}')>0 " +
                $"DROP TABLE [{tableName}];"));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider 無索引表（Heap）應回傳空 Indexes 集合（觸發 ParsePrimaryKey 第一個 early return）")]
        public void SchemaProvider_HeapTableWithNoIndexes_ReturnsSchemaWithEmptyIndexes()
        {
            string tableName = $"tb_cov_noidx_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            DropTable(dbAccess, tableName);

            try
            {
                // Heap 表（無任何索引）→ sys.index_columns 無列
                // GetTableIndexes 回傳空 DataTable → ParsePrimaryKey: table.IsEmpty() = true → 直接 return
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([name] NVARCHAR(50) NOT NULL);"));

                var provider = new SqlTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Empty(schema!.Indexes!);
            }
            finally
            {
                DropTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider 有 UNIQUE 索引但無主鍵的表（觸發 ParsePrimaryKey 第二個 early return）")]
        public void SchemaProvider_TableWithUniqueIndexNoPrimaryKey_ParsesUniqueIndex()
        {
            string tableName = $"tb_cov_nopk_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            DropTable(dbAccess, tableName);

            try
            {
                // UNIQUE NONCLUSTERED INDEX，無 PRIMARY KEY → is_primary_key = 0
                // ParsePrimaryKey: DefaultView.RowFilter="IsPrimaryKey=true" → empty → return（第二個 early return）
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([name] NVARCHAR(50) NOT NULL);"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE UNIQUE NONCLUSTERED INDEX [ux_cov_nopk] ON [{tableName}] ([name]);"));

                var provider = new SqlTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Empty(schema!.Indexes!.Where(i => i.PrimaryKey));
                Assert.Single(schema.Indexes!.Where(i => !i.PrimaryKey && i.Unique));
            }
            finally
            {
                DropTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider NCHAR 欄位長度應取 max_length 原始值（觸發 ParseDbField NCHAR else 分支）")]
        public void SchemaProvider_TableWithNcharColumn_ParseesLengthFromRawBytes()
        {
            string tableName = $"tb_cov_nchar_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            DropTable(dbAccess, tableName);

            try
            {
                // NCHAR(10)：SQL Server max_length = 20（bytes），DbType="nchar" ≠ "NVARCHAR"
                // ParseDbField: IsEquals("nchar","NVARCHAR") = false → else → dbField.Length = 20（原始 bytes）
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] (" +
                    $"  [id] INT NOT NULL, " +
                    $"  [code] NCHAR(10) NOT NULL, " +
                    $"  CONSTRAINT [PK_{tableName}] PRIMARY KEY ([id]));"));

                var provider = new SqlTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var codeField = schema!.Fields!["code"];
                Assert.NotNull(codeField);
                Assert.Equal(FieldDbType.String, codeField.DbType);
                Assert.Equal(20, codeField.Length);
            }
            finally
            {
                DropTable(dbAccess, tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider DECIMAL 欄位應正確解析 Precision 和 Scale（觸發 ParseDbField Decimal 分支）")]
        public void SchemaProvider_TableWithDecimalColumn_ParsesPrecisionAndScale()
        {
            string tableName = $"tb_cov_dec_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            DropTable(dbAccess, tableName);

            try
            {
                // DECIMAL(12,3) → GetFieldDbType → Decimal → ParseDbField 進入 Decimal 分支
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] (" +
                    $"  [id] INT NOT NULL, " +
                    $"  [amount] DECIMAL(12,3) NOT NULL, " +
                    $"  CONSTRAINT [PK_{tableName}] PRIMARY KEY ([id]));"));

                var provider = new SqlTableSchemaProvider(DatabaseId);
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
                DropTable(dbAccess, tableName);
            }
        }
    }
}
