using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqlTableSchemaProviderDbTests
    {
        [DbFact]
        [DisplayName("GetTableSchema NVARCHAR 欄位應正確解析長度（sys.columns.max_length / 2）")]
        public void GetTableSchema_NvarcharColumn_ParsesCharLength()
        {
            string tableName = $"bee_test_nv_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([code] [nvarchar](50) NOT NULL);"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var field = schema!.Fields!.First(f => f.FieldName == "code");
                Assert.Equal(FieldDbType.String, field.DbType);
                Assert.Equal(50, field.Length);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'[{tableName}]') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }

        [DbFact]
        [DisplayName("GetTableSchema Decimal 欄位應正確解析 Precision 與 Scale")]
        public void GetTableSchema_DecimalColumn_ParsesPrecisionAndScale()
        {
            string tableName = $"bee_test_dec_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([amount] [decimal](12,3) NOT NULL DEFAULT (0));"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var field = schema!.Fields!.First(f => f.FieldName == "amount");
                Assert.Equal(FieldDbType.Decimal, field.DbType);
                Assert.Equal(12, field.Precision);
                Assert.Equal(3, field.Scale);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'[{tableName}]') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }

        [DbFact]
        [DisplayName("GetTableSchema 含 IDENTITY 欄位時應解析為 AutoIncrement")]
        public void GetTableSchema_IdentityColumn_ParsesAsAutoIncrement()
        {
            string tableName = $"bee_test_id_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([id] [int] IDENTITY(1,1) NOT NULL);"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var field = schema!.Fields!.First(f => f.FieldName == "id");
                Assert.Equal(FieldDbType.AutoIncrement, field.DbType);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'[{tableName}]') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }

        [DbFact]
        [DisplayName("GetTableSchema 含非 PK 的 unique index 時應正確解析索引清單")]
        public void GetTableSchema_TableWithUniqueIndex_ParsesIndexes()
        {
            string tableName = $"bee_test_ix_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] (" +
                    $"  [id] [uniqueidentifier] NOT NULL CONSTRAINT [pk_{tableName}] PRIMARY KEY, " +
                    $"  [code] [nvarchar](20) NOT NULL" +
                    $");"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE UNIQUE INDEX [uk_{tableName}_code] ON [{tableName}] ([code]);"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var indexes = schema!.Indexes!;
                Assert.True(indexes.Count >= 2);
                Assert.Contains(indexes, ix => ix.PrimaryKey);
                Assert.Contains(indexes, ix => !ix.PrimaryKey && ix.Unique);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'[{tableName}]') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }

        [DbFact]
        [DisplayName("GetTableSchema 含 unique index 但無 PK 時應跳過 PK 解析並處理非 PK 索引")]
        public void GetTableSchema_TableWithIndexButNoPrimaryKey_SkipsPkAndProcessesIndexes()
        {
            string tableName = $"bee_test_nopk_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([code] [nvarchar](20) NOT NULL);"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE UNIQUE INDEX [uk_{tableName}_code] ON [{tableName}] ([code]);"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var indexes = schema!.Indexes!;
                Assert.DoesNotContain(indexes, ix => ix.PrimaryKey);
                Assert.Contains(indexes, ix => ix.Unique);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'[{tableName}]') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }
    }
}
