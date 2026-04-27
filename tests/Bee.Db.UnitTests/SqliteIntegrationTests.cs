using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// SQLite-specific integration tests that exercise the schema provider, schema upgrade
    /// orchestrator and provider-level constraints against a live SQLite database. The
    /// FormMap IUD round-trip is covered separately in
    /// <see cref="FormCommandBuilderIudIntegrationTests"/>.
    /// </summary>
    [Collection("Initialize")]
    public class SqliteIntegrationTests
    {
        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite SchemaProvider 應讀回 fixture 建好的 st_user 表")]
        public void SchemaProvider_ReadsFixtureTable()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            var provider = new Bee.Db.Providers.Sqlite.SqliteTableSchemaProvider(databaseId);

            var schema = provider.GetTableSchema("st_user");

            Assert.NotNull(schema);
            Assert.Equal("st_user", schema!.TableName);
            // sys_no is INTEGER PRIMARY KEY AUTOINCREMENT — read back as AutoIncrement.
            Assert.True(schema.Fields!.Contains("sys_no"));
            Assert.Equal(FieldDbType.AutoIncrement, schema.Fields["sys_no"].DbType);
            Assert.True(schema.Fields.Contains("sys_rowid"));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite SchemaProvider 應對不存在的表回傳 null")]
        public void SchemaProvider_UnknownTable_ReturnsNull()
        {
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            var provider = new Bee.Db.Providers.Sqlite.SqliteTableSchemaProvider(databaseId);

            var schema = provider.GetTableSchema("__no_such_table__");

            Assert.Null(schema);
        }

    }
}
