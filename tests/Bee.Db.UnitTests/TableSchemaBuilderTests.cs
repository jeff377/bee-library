using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Database;
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
    }
}
