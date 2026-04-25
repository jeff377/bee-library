using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqlTableSchemaProviderExtraTests
    {
        [DbFact]
        [DisplayName("GetTableSchema 含 IDENTITY 欄位應解析為 AutoIncrement 型別")]
        public void GetTableSchema_IdentityColumn_ParsesAsAutoIncrement()
        {
            string tableName = $"bee_test_identity_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([id] [int] IDENTITY(1,1) NOT NULL, [name] [nvarchar](50) NOT NULL);"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var idField = schema.Fields!.FirstOrDefault(f => f.FieldName == "id");
                Assert.NotNull(idField);
                Assert.Equal(FieldDbType.AutoIncrement, idField.DbType);
            }
            finally
            {
                var exception = Record.Exception(() =>
                    dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, $"DROP TABLE IF EXISTS [{tableName}]")));
                Assert.Null(exception);
            }
        }

        [DbFact]
        [DisplayName("GetTableSchema 含 DECIMAL 欄位應正確解析 Precision 與 Scale")]
        public void GetTableSchema_DecimalColumn_ParsesPrecisionAndScale()
        {
            string tableName = $"bee_test_decimal_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([amount] [decimal](12,3) NOT NULL);"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var amountField = schema.Fields!.FirstOrDefault(f => f.FieldName == "amount");
                Assert.NotNull(amountField);
                Assert.Equal(FieldDbType.Decimal, amountField.DbType);
                Assert.Equal(12, amountField.Precision);
                Assert.Equal(3, amountField.Scale);
            }
            finally
            {
                var exception = Record.Exception(() =>
                    dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, $"DROP TABLE IF EXISTS [{tableName}]")));
                Assert.Null(exception);
            }
        }

        [DbFact]
        [DisplayName("GetTableSchema 含 NCHAR 欄位應使用欄位原始長度（非除以 2）")]
        public void GetTableSchema_NcharColumn_UsesRawLength()
        {
            string tableName = $"bee_test_nchar_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([code] [nchar](10) NOT NULL);"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                var codeField = schema.Fields!.FirstOrDefault(f => f.FieldName == "code");
                Assert.NotNull(codeField);
                Assert.Equal(FieldDbType.String, codeField.DbType);
                Assert.Equal(10, codeField.Length);
            }
            finally
            {
                var exception = Record.Exception(() =>
                    dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, $"DROP TABLE IF EXISTS [{tableName}]")));
                Assert.Null(exception);
            }
        }

        [DbFact]
        [DisplayName("GetTableSchema 含次要索引的資料表應正確解析索引清單")]
        public void GetTableSchema_WithSecondaryIndex_ParsesIndexes()
        {
            string tableName = $"bee_test_idx_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([id] [uniqueidentifier] NOT NULL, [code] [nvarchar](50) NOT NULL, " +
                    $"CONSTRAINT [pk_{tableName}] PRIMARY KEY ([id] ASC));"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE INDEX [ix_{tableName}_code] ON [{tableName}] ([code] ASC);"));

                var provider = new SqlTableSchemaProvider("common");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.NotNull(schema.Indexes);
                Assert.True(schema.Indexes.Count >= 2);
                Assert.True(schema.Indexes.Any(ix => ix.PrimaryKey));
                Assert.True(schema.Indexes.Any(ix => !ix.PrimaryKey));
            }
            finally
            {
                var exception = Record.Exception(() =>
                    dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, $"DROP TABLE IF EXISTS [{tableName}]")));
                Assert.Null(exception);
            }
        }
    }
}
