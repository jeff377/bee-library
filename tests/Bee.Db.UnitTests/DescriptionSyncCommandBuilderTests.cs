using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers;
using Bee.Db.Providers.MySql;
using Bee.Db.Providers.Oracle;
using Bee.Db.Providers.PostgreSql;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 迴歸：ALTER 路徑一直只替 SQL Server 同步描述，其餘 dialect 直接跳過。ALTER 加進去的
    /// 欄位因此永遠拿不到 caption，而 <see cref="TableSchemaDiff.IsEmpty"/> 把描述差異算進去，
    /// 之後每次比對都回報「有差異」、每次 Plan 都得到零 stage 的 Alter —— 不出錯，但
    /// 「這張表是不是最新的」永遠答否。
    /// </summary>
    public class DescriptionSyncCommandBuilderTests : IClassFixture<SharedDbFixture>
    {
        public DescriptionSyncCommandBuilderTests(SharedDbFixture _) { }

        private static TableSchema BuildDefine(string tableName = "st_demo")
        {
            var schema = new TableSchema { TableName = tableName };
            schema.Fields!.Add("id", "Id", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            return schema;
        }

        /// <summary>建立一份「只加了一個欄位」的 diff，模擬 in-place 升級當下的狀態。</summary>
        private static TableSchemaDiff BuildAddColumnDiff()
        {
            var define = BuildDefine();
            var added = define.Fields!.Add("api_key_id", "API Key", FieldDbType.String, 50);
            var real = BuildDefine();
            var diff = new TableSchemaDiff(define, real);
            diff.Changes.Add(new AddFieldChange(added.Clone()));
            return diff;
        }

        private static TableSchemaDiff BuildCaptionDriftDiff()
        {
            var define = BuildDefine();
            var diff = new TableSchemaDiff(define, BuildDefine());
            diff.DescriptionChanges.Add(new DescriptionChange
            {
                Level = DescriptionLevel.Column,
                FieldName = "name",
                NewValue = "Name",
                IsNew = false,
            });
            return diff;
        }

        // ---------- 加欄位的同一份 plan 就要把 caption 寫進去 ----------

        [Fact]
        [DisplayName("Oracle：ALTER 加欄位時應一併發出該欄的 COMMENT ON COLUMN")]
        public void Oracle_AddedColumn_EmitsColumnComment()
        {
            var statements = new OracleDescriptionSyncCommandBuilder().GetStatements(BuildAddColumnDiff());

            var sql = Assert.Single(statements);
            Assert.Equal("COMMENT ON COLUMN \"ST_DEMO\".\"API_KEY_ID\" IS 'API Key'", sql);
        }

        [Fact]
        [DisplayName("PostgreSQL：ALTER 加欄位時應一併發出該欄的 COMMENT ON COLUMN")]
        public void Pg_AddedColumn_EmitsColumnComment()
        {
            var statements = new PgDescriptionSyncCommandBuilder().GetStatements(BuildAddColumnDiff());

            var sql = Assert.Single(statements);
            Assert.Equal("COMMENT ON COLUMN \"st_demo\".\"api_key_id\" IS 'API Key';", sql);
        }

        [Fact]
        [DisplayName("SQL Server：ALTER 加欄位時應以 sp_addextendedproperty 補上該欄描述")]
        public void SqlServer_AddedColumn_EmitsAddExtendedProperty()
        {
            var statements = new SqlDescriptionSyncCommandBuilder().GetStatements(BuildAddColumnDiff());

            var sql = Assert.Single(statements);
            Assert.Contains("sp_addextendedproperty", sql, StringComparison.Ordinal);
            Assert.Contains("@level2name=N'api_key_id'", sql, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("MySQL：ADD COLUMN 已內含 COMMENT，不應再補一次")]
        public void MySql_AddedColumn_EmitsNothing()
        {
            var statements = new MySqlDescriptionSyncCommandBuilder().GetStatements(BuildAddColumnDiff());

            Assert.Empty(statements);
        }

        // ---------- 純 caption 漂移（沒有結構異動陪同） ----------

        [Fact]
        [DisplayName("Oracle：欄位 caption 漂移應發出 COMMENT ON COLUMN")]
        public void Oracle_CaptionDrift_EmitsColumnComment()
        {
            var statements = new OracleDescriptionSyncCommandBuilder().GetStatements(BuildCaptionDriftDiff());

            Assert.Equal("COMMENT ON COLUMN \"ST_DEMO\".\"NAME\" IS 'Name'", Assert.Single(statements));
        }

        [Fact]
        [DisplayName("MySQL：欄位 caption 漂移應以 MODIFY COLUMN 重下完整定義（含 COMMENT）")]
        public void MySql_CaptionDrift_EmitsModifyColumn()
        {
            var statements = new MySqlDescriptionSyncCommandBuilder().GetStatements(BuildCaptionDriftDiff());

            var sql = Assert.Single(statements);
            Assert.StartsWith("ALTER TABLE `st_demo` MODIFY COLUMN `name` ", sql, StringComparison.Ordinal);
            Assert.Contains("COMMENT 'Name'", sql, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("MySQL：同批已有 AlterFieldChange 的欄位不應再多發一次 MODIFY COLUMN")]
        public void MySql_ColumnAlreadyAltered_SkipsRedundantModify()
        {
            var diff = BuildCaptionDriftDiff();
            var oldField = diff.DefineTable.Fields!["name"].Clone();
            oldField.Length = 30;
            diff.Changes.Add(new AlterFieldChange(oldField, diff.DefineTable.Fields!["name"].Clone()));

            var statements = new MySqlDescriptionSyncCommandBuilder().GetStatements(diff);

            Assert.Empty(statements);
        }

        [Fact]
        [DisplayName("MySQL：AutoIncrement 欄位的 MODIFY COLUMN 必須保留 AUTO_INCREMENT")]
        public void MySql_AutoIncrementCaptionDrift_KeepsAutoIncrement()
        {
            // 迴歸：MODIFY 會整段換掉欄位定義，漏掉 AUTO_INCREMENT 就等於把自增拔掉，
            // 之後每次 INSERT 都是 "Field 'sys_no' doesn't have a default value"。
            var define = new TableSchema { TableName = "st_demo" };
            define.Fields!.Add("sys_no", "Sequence", FieldDbType.AutoIncrement);
            var diff = new TableSchemaDiff(define, define.Clone());
            diff.DescriptionChanges.Add(new DescriptionChange
            {
                Level = DescriptionLevel.Column,
                FieldName = "sys_no",
                NewValue = "Sequence",
                IsNew = true,
            });

            var sql = Assert.Single(new MySqlDescriptionSyncCommandBuilder().GetStatements(diff));

            Assert.Equal("ALTER TABLE `st_demo` MODIFY COLUMN `sys_no` BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Sequence';", sql);
        }

        [Fact]
        [DisplayName("MySQL：表層 DisplayName 漂移應發出 ALTER TABLE ... COMMENT")]
        public void MySql_TableDescriptionDrift_EmitsTableComment()
        {
            var diff = new TableSchemaDiff(BuildDefine(), BuildDefine());
            diff.DescriptionChanges.Add(new DescriptionChange
            {
                Level = DescriptionLevel.Table,
                NewValue = "示範資料表",
                IsNew = true,
            });

            var statements = new MySqlDescriptionSyncCommandBuilder().GetStatements(diff);

            Assert.Equal("ALTER TABLE `st_demo` COMMENT = '示範資料表';", Assert.Single(statements));
        }

        [Fact]
        [DisplayName("無描述可寫時各 dialect 皆不產生語句")]
        public void AllDialects_NothingToApply_EmitNoStatements()
        {
            var define = new TableSchema { TableName = "st_demo" };
            define.Fields!.Add("id", string.Empty, FieldDbType.Guid);
            var diff = new TableSchemaDiff(define, define.Clone());

            Assert.Empty(new OracleDescriptionSyncCommandBuilder().GetStatements(diff));
            Assert.Empty(new PgDescriptionSyncCommandBuilder().GetStatements(diff));
            Assert.Empty(new MySqlDescriptionSyncCommandBuilder().GetStatements(diff));
            Assert.Empty(new SqlDescriptionSyncCommandBuilder().GetStatements(diff));
        }

        // ---------- dialect factory 接線 ----------

        [Theory]
        [DisplayName("能持久化描述的 dialect factory 都要提供 description sync builder")]
        [InlineData(typeof(SqlDialectFactory))]
        [InlineData(typeof(PgDialectFactory))]
        [InlineData(typeof(MySqlDialectFactory))]
        [InlineData(typeof(OracleDialectFactory))]
        public void DialectFactory_DescriptionCapableDialects_ReturnBuilder(Type factoryType)
        {
            var factory = (IDialectFactory)Activator.CreateInstance(factoryType)!;

            Assert.NotNull(factory.CreateDescriptionSyncCommandBuilder());
        }

        [Fact]
        [DisplayName("SQLite 無 COMMENT 機制，不提供 description sync builder")]
        public void DialectFactory_Sqlite_ReturnsNull()
        {
            // 只透過介面呼叫：這是 default interface member，SqliteDialectFactory 未覆寫。
            IDialectFactory factory = new SqliteDialectFactory();

            Assert.Null(factory.CreateDescriptionSyncCommandBuilder());
        }

        [Fact]
        [DisplayName("SQLite：caption 差異不應算成 diff（否則永遠不會是 NoChange）")]
        public void CompareToDiff_Sqlite_DoesNotReportDescriptionDrift()
        {
            var define = BuildDefine();
            // SqliteTableSchemaProvider 一律把 caption 讀回空字串。
            var real = new TableSchema { TableName = "st_demo" };
            real.Fields!.Add("id", string.Empty, FieldDbType.Guid);
            real.Fields!.Add("name", string.Empty, FieldDbType.String, 50);

            var diff = new TableSchemaComparer(define, real, DatabaseType.SQLite).CompareToDiff();

            Assert.Empty(diff.DescriptionChanges);
            Assert.True(diff.IsEmpty);
        }
    }
}
