using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.MySql;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-syntax tests for <see cref="MySqlTableAlterCommandBuilder"/>. No live database
    /// connection — verifies the routing of <see cref="ITableChange"/> kinds to MySQL 8.0+
    /// dialect output (backtick quoting, MODIFY COLUMN re-definition, PK via DROP PRIMARY KEY).
    /// </summary>
    public class MySqlTableAlterCommandBuilderTests
    {
        private readonly MySqlTableAlterCommandBuilder _builder = new MySqlTableAlterCommandBuilder();

        private static TableSchemaIndex BuildIndex(string indexName, string field, bool unique)
        {
            var schema = new TableSchema { TableName = "st_demo" };
            return schema.Indexes!.Add(indexName, field, unique);
        }

        private static TableSchemaIndex BuildPrimaryKey(string field)
        {
            var schema = new TableSchema { TableName = "st_demo" };
            return schema.Indexes!.AddPrimaryKey(field);
        }

        // ---------- ExecutionKind ----------

        [Fact]
        [DisplayName("MySQL GetExecutionKind：AddFieldChange 應為 Alter")]
        public void GetExecutionKind_AddField_IsAlter()
        {
            var change = new AddFieldChange(new DbField("col", "Col", FieldDbType.Integer));
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("MySQL GetExecutionKind：RenameFieldChange 應為 Alter")]
        public void GetExecutionKind_Rename_IsAlter()
        {
            var change = new RenameFieldChange("oldname", new DbField("newname", "New", FieldDbType.String));
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("MySQL GetExecutionKind：AlterFieldChange 同 family（Integer→Long）應為 Alter")]
        public void GetExecutionKind_AlterFieldSameFamily_IsAlter()
        {
            var oldField = new DbField("col", "Col", FieldDbType.Integer);
            var newField = new DbField("col", "Col", FieldDbType.Long);
            var change = new AlterFieldChange(oldField, newField);
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("MySQL GetExecutionKind：AlterFieldChange 跨 family（Integer→String）應為 Rebuild")]
        public void GetExecutionKind_AlterFieldCrossFamily_IsRebuild()
        {
            var oldField = new DbField("col", "Col", FieldDbType.Integer);
            var newField = new DbField("col", "Col", FieldDbType.String) { Length = 50 };
            var change = new AlterFieldChange(oldField, newField);
            Assert.Equal(ChangeExecutionKind.Rebuild, _builder.GetExecutionKind(change));
        }

        // ---------- IsNarrowingChange ----------

        [Fact]
        [DisplayName("MySQL IsNarrowingChange：String 縮短應回傳 true")]
        public void IsNarrowingChange_StringShortened_ReturnsTrue()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            Assert.True(_builder.IsNarrowingChange(new AlterFieldChange(oldField, newField)));
        }

        [Fact]
        [DisplayName("MySQL IsNarrowingChange：String 加長應回傳 false")]
        public void IsNarrowingChange_StringExtended_ReturnsFalse()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            Assert.False(_builder.IsNarrowingChange(new AlterFieldChange(oldField, newField)));
        }

        // ---------- GetStatements ----------

        [Fact]
        [DisplayName("MySQL GetStatements：AddField 產生 ALTER TABLE ADD COLUMN（backtick 識別符）")]
        public void GetStatements_AddField_EmitsAlterTableAddColumn()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer) { AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AddFieldChange(field));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE `st_demo` ADD COLUMN", sql);
            Assert.Contains("`age` INT NOT NULL DEFAULT 0", sql);
        }

        [Fact]
        [DisplayName("MySQL GetStatements：AlterField 產生 MODIFY COLUMN 並包含完整 column 定義")]
        public void GetStatements_AlterField_EmitsModifyColumn()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = false };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100, AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE `st_demo` MODIFY COLUMN", sql);
            Assert.Contains("`name` VARCHAR(100) NOT NULL", sql);
        }

        [Fact]
        [DisplayName("MySQL GetStatements：RenameField 產生 RENAME COLUMN（backtick）")]
        public void GetStatements_RenameField_EmitsRenameColumn()
        {
            var change = new RenameFieldChange("oldname", new DbField("newname", "New", FieldDbType.String) { Length = 50 });
            var statements = _builder.GetStatements("st_demo", change);

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE `st_demo` RENAME COLUMN `oldname` TO `newname`;", sql);
        }

        [Fact]
        [DisplayName("MySQL GetStatements：AddIndex 非 PK 產生 CREATE INDEX")]
        public void GetStatements_AddIndex_NonPk_EmitsCreateIndex()
        {
            var index = BuildIndex("ix_{0}_col", "col", unique: false);
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Contains("CREATE INDEX `ix_st_demo_col` ON `st_demo`", sql);
        }

        [Fact]
        [DisplayName("MySQL GetStatements：AddIndex 唯一索引產生 CREATE UNIQUE INDEX")]
        public void GetStatements_AddIndex_Unique_EmitsCreateUniqueIndex()
        {
            var index = BuildIndex("uk_{0}_col", "col", unique: true);
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Contains("CREATE UNIQUE INDEX `uk_st_demo_col` ON `st_demo`", sql);
        }

        [Fact]
        [DisplayName("MySQL GetStatements：AddIndex PK 產生 ADD CONSTRAINT PRIMARY KEY")]
        public void GetStatements_AddIndex_Pk_EmitsAddConstraint()
        {
            var pk = BuildPrimaryKey("id");
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(pk));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE `st_demo` ADD CONSTRAINT", sql);
            Assert.Contains("PRIMARY KEY (`id`", sql);
        }

        [Fact]
        [DisplayName("MySQL GetStatements：DropIndex 非 PK 產生 DROP INDEX ON")]
        public void GetStatements_DropIndex_NonPk_EmitsDropIndexOn()
        {
            var index = BuildIndex("ix_{0}_col", "col", unique: false);
            // 模擬：當作既有 index name（已 resolve），略過 {0} 替換
            index.Name = "ix_st_demo_col";
            var statements = _builder.GetStatements("st_demo", new DropIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("DROP INDEX `ix_st_demo_col` ON `st_demo`;", sql);
        }

        [Fact]
        [DisplayName("MySQL GetStatements：DropIndex PK 產生 DROP PRIMARY KEY")]
        public void GetStatements_DropIndex_Pk_EmitsDropPrimaryKey()
        {
            var pk = BuildPrimaryKey("id");
            var statements = _builder.GetStatements("st_demo", new DropIndexChange(pk));

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE `st_demo` DROP PRIMARY KEY;", sql);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("MySQL GetStatements：空白 tableName 應拋例外")]
        public void GetStatements_EmptyTableName_Throws(string? tableName)
        {
            var change = new AddFieldChange(new DbField("col", "Col", FieldDbType.Integer));
            Assert.ThrowsAny<ArgumentException>(() => _builder.GetStatements(tableName!, change));
        }
    }
}
