using System.ComponentModel;
using Bee.Db.Providers.SqlServer;
using Bee.Tests.Shared;
using Bee.Definition.Database;
using Bee.Base.Data;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="SqlTableSchemaProvider"/> 需要活 SQL Server 連線的 ParseIndexes 與
    /// ParsePrimaryKey 未涵蓋路徑，以及 GetFieldDbType null 輸入邊緣案例。
    /// </summary>
    [Collection("Initialize")]
    public class SqlTableSchemaProviderIndexTests
    {
        #region GetFieldDbType 邊緣案例

        [Fact]
        [DisplayName("SQL Server GetFieldDbType null 輸入應回傳 Unknown")]
        public void GetFieldDbType_NullInput_ReturnsUnknown()
        {
            Assert.Equal(FieldDbType.Unknown, SqlTableSchemaProvider.GetFieldDbType(null!, 0, 0, 0));
        }

        #endregion

        #region 整合測試（需 SQL Server 連線）

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider 應正確讀取包含非主鍵索引的資料表結構（覆蓋 ParseIndexes 迴圈主體）")]
        public void GetTableSchema_TableWithNonPkIndex_ParseIndexesLoopBodyCovered()
        {
            string tableName = $"bee_idx_edge_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common_sqlserver");

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] (" +
                    "[sys_rowid] UNIQUEIDENTIFIER NOT NULL," +
                    "[code] NVARCHAR(20) NOT NULL," +
                    "CONSTRAINT [PK_{tableName}] PRIMARY KEY ([sys_rowid])" +
                    ");"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE UNIQUE INDEX [IX_{tableName}_code] ON [{tableName}] ([code]);"));

                var provider = new SqlTableSchemaProvider("common_sqlserver");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.True(schema!.Indexes!.Count >= 2, "應有 PK + 至少一個非主鍵唯一索引");
                Assert.True(schema.Indexes.Any(idx => idx.PrimaryKey), "應有主鍵索引");
                Assert.True(schema.Indexes.Any(idx => !idx.PrimaryKey && idx.Unique), "應有非主鍵唯一索引");
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'[{tableName}]', N'U') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider 讀取無主鍵資料表時 ParsePrimaryKey 應提早返回")]
        public void GetTableSchema_TableWithNoPrimaryKey_ParsePrimaryKeyEarlyReturn()
        {
            string tableName = $"bee_nopk_edge_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common_sqlserver");

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([code] NVARCHAR(20) NOT NULL);"));

                var provider = new SqlTableSchemaProvider("common_sqlserver");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.False(schema!.Indexes!.Any(idx => idx.PrimaryKey), "無主鍵資料表不應有主鍵索引");
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'[{tableName}]', N'U') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server SchemaProvider NVARCHAR(MAX) 欄位應映射為 Text 型別")]
        public void GetTableSchema_NvarcharMaxColumn_MapsToTextType()
        {
            string tableName = $"bee_nvarmax_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common_sqlserver");

            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([content] NVARCHAR(MAX) NULL);"));

                var provider = new SqlTableSchemaProvider("common_sqlserver");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.True(schema!.Fields!.Contains("content"), "應包含 content 欄位");
                Assert.Equal(FieldDbType.Text, schema.Fields["content"].DbType);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'[{tableName}]', N'U') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }

        #endregion
    }
}
