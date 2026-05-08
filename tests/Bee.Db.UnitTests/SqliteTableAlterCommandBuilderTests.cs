using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqliteTableAlterCommandBuilderTests
    {
        private readonly SqliteTableAlterCommandBuilder _builder = new();

        /// <summary>
        /// Test-only fake to drive the default branches in <c>GetExecutionKind</c> and
        /// <c>GetStatements</c>; production <see cref="ITableChange"/> implementations are all
        /// matched by the switch arms above the default case.
        /// </summary>
        private sealed class UnknownChange : ITableChange
        {
            public string Describe() => "unknown";
        }

        // ---------- GetExecutionKind ----------

        [Fact]
        [DisplayName("SQLite GetExecutionKind：AddFieldChange 應為 Alter")]
        public void GetExecutionKind_AddField_ReturnsAlter()
        {
            var change = new AddFieldChange(new DbField("age", "Age", FieldDbType.Integer));
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("SQLite GetExecutionKind：RenameFieldChange 應為 Alter")]
        public void GetExecutionKind_RenameField_ReturnsAlter()
        {
            var change = new RenameFieldChange("oldname", new DbField("newname", "New", FieldDbType.String));
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("SQLite GetExecutionKind：AddIndexChange 應為 Alter")]
        public void GetExecutionKind_AddIndex_ReturnsAlter()
        {
            var index = new TableSchemaIndex { Name = "ix_demo_name" };
            index.IndexFields!.Add("name");
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(new AddIndexChange(index)));
        }

        [Fact]
        [DisplayName("SQLite GetExecutionKind：DropIndexChange 應為 Alter")]
        public void GetExecutionKind_DropIndex_ReturnsAlter()
        {
            var index = new TableSchemaIndex { Name = "ix_demo_name" };
            index.IndexFields!.Add("name");
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(new DropIndexChange(index)));
        }

        [Fact]
        [DisplayName("SQLite GetExecutionKind：AlterFieldChange 同 family 應為 Rebuild")]
        public void GetExecutionKind_AlterFieldSameFamily_ReturnsRebuild()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            Assert.Equal(ChangeExecutionKind.Rebuild,
                _builder.GetExecutionKind(new AlterFieldChange(oldField, newField)));
        }

        [Fact]
        [DisplayName("SQLite GetExecutionKind：AlterFieldChange 跨 family 應為 Rebuild")]
        public void GetExecutionKind_AlterFieldCrossFamily_ReturnsRebuild()
        {
            var oldField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            var newField = new DbField("v", "V", FieldDbType.Integer);
            Assert.Equal(ChangeExecutionKind.Rebuild,
                _builder.GetExecutionKind(new AlterFieldChange(oldField, newField)));
        }

        [Fact]
        [DisplayName("SQLite GetExecutionKind：AlterFieldChange 牽涉 Unknown 應為 NotSupported")]
        public void GetExecutionKind_AlterFieldUnknown_ReturnsNotSupported()
        {
            var oldField = new DbField("v", "V", FieldDbType.Unknown);
            var newField = new DbField("v", "V", FieldDbType.Integer);
            Assert.Equal(ChangeExecutionKind.NotSupported,
                _builder.GetExecutionKind(new AlterFieldChange(oldField, newField)));
        }

        // ---------- IsNarrowingChange ----------

        [Fact]
        [DisplayName("SQLite IsNarrowingChange：String 縮短應回傳 true")]
        public void IsNarrowingChange_StringShortened_ReturnsTrue()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            Assert.True(_builder.IsNarrowingChange(new AlterFieldChange(oldField, newField)));
        }

        [Fact]
        [DisplayName("SQLite IsNarrowingChange：非 AlterField 應回傳 false")]
        public void IsNarrowingChange_NonAlterChange_ReturnsFalse()
        {
            var change = new AddFieldChange(new DbField("age", "Age", FieldDbType.Integer));
            Assert.False(_builder.IsNarrowingChange(change));
        }

        // ---------- Statements ----------

        [Fact]
        [DisplayName("SQLite GetStatements：AddField 產生 ALTER TABLE ADD COLUMN")]
        public void GetStatements_AddField_EmitsAlterTableAddColumn()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer) { AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AddFieldChange(field));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE \"st_demo\" ADD COLUMN", sql);
            Assert.Contains("\"age\" INTEGER NOT NULL", sql);
            Assert.Contains("DEFAULT", sql);
        }

        [Fact]
        [DisplayName("SQLite GetStatements：AddField 文字欄位應帶 COLLATE NOCASE（與 CREATE TABLE 一致）")]
        public void GetStatements_AddStringField_IncludesCollateNocase()
        {
            // 由於 CREATE 與 ALTER 共用 SqliteSchemaSyntax.GetColumnDefinition，
            // ALTER TABLE ADD COLUMN 新增文字欄位也會自動帶 COLLATE NOCASE，與 CREATE 行為一致。
            var field = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AddFieldChange(field));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE \"st_demo\" ADD COLUMN", sql);
            Assert.Contains("\"name\" VARCHAR(50) COLLATE NOCASE NOT NULL", sql);
        }

        [Fact]
        [DisplayName("SQLite GetStatements：RenameField 產生 RENAME COLUMN")]
        public void GetStatements_RenameField_EmitsRenameColumn()
        {
            var change = new RenameFieldChange("oldname", new DbField("newname", "New", FieldDbType.String) { Length = 50 });
            var statements = _builder.GetStatements("st_demo", change);

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE \"st_demo\" RENAME COLUMN \"oldname\" TO \"newname\";", sql);
        }

        [Fact]
        [DisplayName("SQLite GetStatements：AddIndex 產生 CREATE INDEX")]
        public void GetStatements_AddIndex_EmitsCreateIndex()
        {
            var index = new TableSchemaIndex { Name = "ix_{0}_col" };
            index.IndexFields!.Add("col");

            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("CREATE INDEX \"ix_st_demo_col\" ON \"st_demo\" (\"col\" ASC);", sql);
        }

        [Fact]
        [DisplayName("SQLite GetStatements：AddIndex Unique 應產生 CREATE UNIQUE INDEX")]
        public void GetStatements_AddIndexUnique_EmitsCreateUniqueIndex()
        {
            var index = new TableSchemaIndex { Name = "uk_{0}_col", Unique = true };
            index.IndexFields!.Add("col");

            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            Assert.Contains("CREATE UNIQUE INDEX", statements[0]);
        }

        [Fact]
        [DisplayName("SQLite GetStatements：AddIndex 帶 PrimaryKey 應擲 NotSupportedException")]
        public void GetStatements_AddPrimaryKeyIndex_Throws()
        {
            var index = new TableSchemaIndex { Name = "pk_st_demo", PrimaryKey = true };
            index.IndexFields!.Add("sys_rowid");

            Assert.Throws<NotSupportedException>(() =>
                _builder.GetStatements("st_demo", new AddIndexChange(index)));
        }

        [Fact]
        [DisplayName("SQLite GetStatements：DropIndex 產生 DROP INDEX")]
        public void GetStatements_DropIndex_EmitsDropIndex()
        {
            var index = new TableSchemaIndex { Name = "ix_st_demo_col" };
            index.IndexFields!.Add("col");

            var statements = _builder.GetStatements("st_demo", new DropIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("DROP INDEX \"ix_st_demo_col\";", sql);
        }

        [Fact]
        [DisplayName("SQLite GetStatements：DropIndex 帶 PrimaryKey 應擲 NotSupportedException")]
        public void GetStatements_DropPrimaryKeyIndex_Throws()
        {
            var index = new TableSchemaIndex { Name = "pk_st_demo", PrimaryKey = true };
            index.IndexFields!.Add("sys_rowid");

            Assert.Throws<NotSupportedException>(() =>
                _builder.GetStatements("st_demo", new DropIndexChange(index)));
        }

        [Fact]
        [DisplayName("SQLite GetStatements：AlterField 應擲 InvalidOperationException（必須走 rebuild 路徑）")]
        public void GetStatements_AlterField_Throws()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };

            Assert.Throws<InvalidOperationException>(() =>
                _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField)));
        }

        [Fact]
        [DisplayName("SQLite GetExecutionKind：未知 ITableChange 子類應回傳 NotSupported")]
        public void GetExecutionKind_UnknownChange_ReturnsNotSupported()
        {
            Assert.Equal(ChangeExecutionKind.NotSupported, _builder.GetExecutionKind(new UnknownChange()));
        }

        [Fact]
        [DisplayName("SQLite GetStatements：未知 ITableChange 子類應擲 InvalidOperationException")]
        public void GetStatements_UnknownChange_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _builder.GetStatements("st_demo", new UnknownChange()));
            Assert.Contains("Unsupported change type", ex.Message);
        }
    }
}
