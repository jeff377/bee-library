using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Ddl;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Schema;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 純語法測試：覆蓋 <see cref="SqliteDialectFactory"/> 的 factory 方法。
    /// 只驗證回傳的具體型別與委派至 SqliteSchemaHelper.GetDefaultValueExpression 的一致性，
    /// 不觸及任何資料庫連線。
    /// </summary>
    [Collection("Initialize")]
    public class SqliteDialectFactoryTests
    {
        private readonly SqliteDialectFactory _factory = new();

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SqliteDialectFactory：CreateTableSchemaProvider 應回傳 SqliteTableSchemaProvider")]
        public void CreateTableSchemaProvider_ReturnsSqliteImpl()
        {
            // 需要實際 databaseId（DbAccess 建構需查 connection registry），改以 DbFact 限制執行條件。
            var databaseId = TestDbConventions.GetDatabaseId(DatabaseType.SQLite);
            ITableSchemaProvider provider = _factory.CreateTableSchemaProvider(databaseId);
            Assert.IsType<SqliteTableSchemaProvider>(provider);
        }

        [Fact]
        [DisplayName("SqliteDialectFactory：CreateCreateTableCommandBuilder 應回傳 SqliteCreateTableCommandBuilder")]
        public void CreateCreateTableCommandBuilder_ReturnsSqliteImpl()
        {
            ICreateTableCommandBuilder builder = _factory.CreateCreateTableCommandBuilder();
            Assert.IsType<SqliteCreateTableCommandBuilder>(builder);
        }

        [Fact]
        [DisplayName("SqliteDialectFactory：CreateTableAlterCommandBuilder 應回傳 SqliteTableAlterCommandBuilder")]
        public void CreateTableAlterCommandBuilder_ReturnsSqliteImpl()
        {
            ITableAlterCommandBuilder builder = _factory.CreateTableAlterCommandBuilder();
            Assert.IsType<SqliteTableAlterCommandBuilder>(builder);
        }

        [Fact]
        [DisplayName("SqliteDialectFactory：CreateTableRebuildCommandBuilder 應回傳 SqliteTableRebuildCommandBuilder")]
        public void CreateTableRebuildCommandBuilder_ReturnsSqliteImpl()
        {
            ITableRebuildCommandBuilder builder = _factory.CreateTableRebuildCommandBuilder();
            Assert.IsType<SqliteTableRebuildCommandBuilder>(builder);
        }

        [Fact]
        [DisplayName("SqliteDialectFactory：CreateFormCommandBuilder 找不到 progId 應擲 FileNotFoundException")]
        public void CreateFormCommandBuilder_UnknownProgId_Throws()
        {
            // Factory delegates to SqliteFormCommandBuilder(progID); the underlying GetFormSchema
            // throws when the form file is missing. This still drives line coverage of the factory.
            Assert.Throws<System.IO.FileNotFoundException>(() => _factory.CreateFormCommandBuilder("__not_exists__"));
        }

        [Theory]
        [InlineData(FieldDbType.String, "")]
        [InlineData(FieldDbType.Integer, "0")]
        [InlineData(FieldDbType.DateTime, "CURRENT_TIMESTAMP")]
        [InlineData(FieldDbType.Guid, "(hex(randomblob(16)))")]
        [DisplayName("SqliteDialectFactory：GetDefaultValueExpression 應委派至 SqliteSchemaHelper")]
        public void GetDefaultValueExpression_DelegatesToHelper(FieldDbType dbType, string expected)
        {
            Assert.Equal(expected, _factory.GetDefaultValueExpression(dbType));
        }
    }
}
