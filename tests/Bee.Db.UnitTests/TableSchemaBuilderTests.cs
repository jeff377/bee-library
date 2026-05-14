using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    public class TableSchemaBuilderTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public TableSchemaBuilderTests(SharedDbFixture fx) { _fx = fx; }
        private TableSchemaBuilder NewBuilder(string databaseId)
            => new(databaseId, _fx.GetRequiredService<IDefineAccess>(), _fx.GetRequiredService<IDbConnectionManager>());

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("TableSchemaBuilder 比對結構一致的資料表應回傳 None")]
        public void Compare_UpToDateTable_ReturnsNoneAction()
        {
            var builder = NewBuilder("common_sqlserver");
            var result = builder.Compare("common", "st_user");

            Assert.NotNull(result);
            Assert.Equal(DbUpgradeAction.None, result.UpgradeAction);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("TableSchemaBuilder 取得命令文字應回傳空字串（結構已同步）")]
        public void GetCommandText_UpToDateTable_ReturnsEmpty()
        {
            var builder = NewBuilder("common_sqlserver");
            string sql = builder.GetCommandText("common", "st_user");

            Assert.Equal(string.Empty, sql);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("TableSchemaBuilder Execute 結構已同步時應回傳 false")]
        public void Execute_UpToDateTable_ReturnsFalse()
        {
            var builder = NewBuilder("common_sqlserver");
            bool upgraded = builder.Execute("common", "st_user");

            Assert.False(upgraded);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("TableSchemaBuilder GetCommandText 對 company 類別已建立的 ft_project 應回傳空字串")]
        public void GetCommandText_CompanyCategoryUpToDate_SqlServer_ReturnsEmpty()
        {
            // SharedDbFixture 已透過 SharedDatabaseState 在 SQL Server 上建立 company 類別的
            // ft_project；故 diff 結果應為空。
            var builder = NewBuilder(TestDbConventions.GetDatabaseId(DatabaseType.SQLServer, "company"));
            string sql = builder.GetCommandText("company", "ft_project");
            Assert.Equal(string.Empty, sql);
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("TableSchemaBuilder GetCommandText PostgreSQL 對 company 類別已建立的 ft_project 應回傳空字串")]
        public void GetCommandText_CompanyCategoryUpToDate_PostgreSql_ReturnsEmpty()
        {
            var builder = NewBuilder(TestDbConventions.GetDatabaseId(DatabaseType.PostgreSQL, "company"));
            string sql = builder.GetCommandText("company", "ft_project");
            Assert.Equal(string.Empty, sql);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("TableSchemaBuilder GetCommandText SQL Server 有欄位差異時應回傳非空 SQL 升級指令")]
        public void GetCommandText_SchemaDrift_SqlServer_ReturnsNonEmptySql()
        {
            const string tableName = "zz_tsb_getcommand_test";
            const string databaseId = "common_sqlserver";
            var db = _fx.NewDbAccess(databaseId);
            db.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"IF OBJECT_ID(N'{tableName}', N'U') IS NOT NULL DROP TABLE [{tableName}]; " +
                $"CREATE TABLE [{tableName}] ([sys_rowid] uniqueidentifier NOT NULL, " +
                $"CONSTRAINT [pk_{tableName}] PRIMARY KEY ([sys_rowid]));"));
            try
            {
                var builder = new TableSchemaBuilder(
                    databaseId,
                    new StubDefineAccess(BuildDriftSchema(tableName)),
                    _fx.GetRequiredService<IDbConnectionManager>());

                string sql = builder.GetCommandText("test", tableName);

                Assert.NotEmpty(sql);
            }
            finally
            {
                db.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF OBJECT_ID(N'{tableName}', N'U') IS NOT NULL DROP TABLE [{tableName}];"));
            }
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("TableSchemaBuilder GetCommandText PostgreSQL 有欄位差異時應回傳非空 SQL 升級指令")]
        public void GetCommandText_SchemaDrift_PostgreSql_ReturnsNonEmptySql()
        {
            const string tableName = "zz_tsb_getcommand_test";
            const string databaseId = "common_postgresql";
            var db = _fx.NewDbAccess(databaseId);
            db.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                $"DROP TABLE IF EXISTS {tableName}; " +
                $"CREATE TABLE {tableName} (sys_rowid uuid NOT NULL, " +
                $"CONSTRAINT pk_{tableName} PRIMARY KEY (sys_rowid));"));
            try
            {
                var builder = new TableSchemaBuilder(
                    databaseId,
                    new StubDefineAccess(BuildDriftSchema(tableName)),
                    _fx.GetRequiredService<IDbConnectionManager>());

                string sql = builder.GetCommandText("test", tableName);

                Assert.NotEmpty(sql);
            }
            finally
            {
                db.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS {tableName};"));
            }
        }

        private static TableSchema BuildDriftSchema(string tableName)
        {
            var schema = new TableSchema { TableName = tableName };
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            schema.Indexes!.AddPrimaryKey(SysFields.RowId);
            return schema;
        }

        private sealed class StubDefineAccess : IDefineAccess
        {
            private readonly TableSchema _schema;
            public StubDefineAccess(TableSchema schema) { _schema = schema; }

            public TableSchema GetTableSchema(string categoryId, string tableName) => _schema;

            public object GetDefine(DefineType defineType, string[]? keys = null) => throw new NotSupportedException();
            public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw new NotSupportedException();
            public SystemSettings GetSystemSettings() => throw new NotSupportedException();
            public void SaveSystemSettings(SystemSettings settings) => throw new NotSupportedException();
            public DatabaseSettings GetDatabaseSettings() => throw new NotSupportedException();
            public void SaveDatabaseSettings(DatabaseSettings settings) => throw new NotSupportedException();
            public ProgramSettings GetProgramSettings() => throw new NotSupportedException();
            public void SaveProgramSettings(ProgramSettings settings) => throw new NotSupportedException();
            public DbCategorySettings GetDbCategorySettings() => throw new NotSupportedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotSupportedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotSupportedException();
            public FormSchema GetFormSchema(string progId) => throw new NotSupportedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotSupportedException();
            public FormLayout GetFormLayout(string layoutId) => throw new NotSupportedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotSupportedException();
        }
    }
}
