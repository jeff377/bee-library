using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Manager;
using Bee.Db.Providers;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbDialectRegistryTests
    {
        [Fact]
        [DisplayName("Register + Get 應成功取回對應的工廠")]
        public void RegisterAndGet_ReturnsSameFactory()
        {
            var factory = new SqlDialectFactory();
            DbDialectRegistry.Register(DatabaseType.SQLServer, factory);

            Assert.Same(factory, DbDialectRegistry.Get(DatabaseType.SQLServer));
        }

        [Fact]
        [DisplayName("IsRegistered 在已註冊時應回傳 true")]
        public void IsRegistered_Registered_ReturnsTrue()
        {
            DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());

            Assert.True(DbDialectRegistry.IsRegistered(DatabaseType.SQLServer));
        }

        [Fact]
        [DisplayName("Register 傳 null 應擲 ArgumentNullException")]
        public void Register_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DbDialectRegistry.Register(DatabaseType.SQLServer, null!));
        }

        [Fact]
        [DisplayName("Get 未註冊型別應擲 KeyNotFoundException")]
        public void Get_Unregistered_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => DbDialectRegistry.Get(DatabaseType.Oracle));
        }
    }

    [Collection("Initialize")]
    public class SqlDialectFactoryTests
    {
        private readonly SqlDialectFactory _factory = new();

        [Fact]
        [DisplayName("CreateTableSchemaProvider 應回傳 SqlTableSchemaProvider")]
        public void CreateTableSchemaProvider_ReturnsSqlImpl()
        {
            var provider = _factory.CreateTableSchemaProvider("common");

            Assert.IsType<SqlTableSchemaProvider>(provider);
            Assert.Equal("common", provider.DatabaseId);
        }

        [Fact]
        [DisplayName("CreateCreateTableCommandBuilder 應回傳 SqlCreateTableCommandBuilder")]
        public void CreateCreateTableCommandBuilder_ReturnsSqlImpl()
        {
            Assert.IsType<SqlCreateTableCommandBuilder>(_factory.CreateCreateTableCommandBuilder());
        }

        [Fact]
        [DisplayName("CreateTableAlterCommandBuilder 應回傳 SqlTableAlterCommandBuilder")]
        public void CreateTableAlterCommandBuilder_ReturnsSqlImpl()
        {
            Assert.IsType<SqlTableAlterCommandBuilder>(_factory.CreateTableAlterCommandBuilder());
        }

        [Fact]
        [DisplayName("CreateTableRebuildCommandBuilder 應實作 ITableRebuildCommandBuilder")]
        public void CreateTableRebuildCommandBuilder_ImplementsInterface()
        {
            var rebuildBuilder = _factory.CreateTableRebuildCommandBuilder();

            Assert.IsAssignableFrom<ITableRebuildCommandBuilder>(rebuildBuilder);
        }

[Fact]
        [DisplayName("GetDefaultValueExpression 應回傳 SQL Server 特有預設值（如 getdate、newid）")]
        public void GetDefaultValueExpression_SqlServerDefaults()
        {
            Assert.Equal("getdate()", _factory.GetDefaultValueExpression(FieldDbType.DateTime));
            Assert.Equal("newid()", _factory.GetDefaultValueExpression(FieldDbType.Guid));
            Assert.Equal("0", _factory.GetDefaultValueExpression(FieldDbType.Integer));
            Assert.Equal(string.Empty, _factory.GetDefaultValueExpression(FieldDbType.String));
        }

        [Fact]
        [DisplayName("CreateTableRebuildCommandBuilder 回傳實例可處理 diff（煙霧測試）")]
        public void CreateTableRebuildCommandBuilder_CanProduceSql()
        {
            var define = new TableSchema { TableName = "st_sample" };
            define.Fields!.Add("id", "Id", FieldDbType.Guid);
            var real = new TableSchema { TableName = "st_sample" };
            real.Fields!.Add("id", "Id", FieldDbType.Guid);
            var diff = new TableSchemaComparer(define, real).CompareToDiff();
            // 強制加一筆變化讓 rebuild 產出非空 SQL
            diff.Changes.Add(new AddFieldChange(new DbFieldForTest()));

            var builder = _factory.CreateTableRebuildCommandBuilder();
            var sql = builder.GetCommandText(diff);

            Assert.Contains("tmp_st_sample", sql);
        }

        // 測試用 helper（避免為了單一煙霧測試依賴較重的 schema 建構）
        private sealed class DbFieldForTest : global::Bee.Definition.Database.DbField
        {
            public DbFieldForTest() : base("note", "Note", FieldDbType.String)
            {
                Length = 10;
            }
        }
    }
}
