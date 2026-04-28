using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-syntax tests for <see cref="OracleTableAlterCommandBuilder"/>. No live database
    /// connection — verifies the routing of <see cref="ITableChange"/> kinds to Oracle 19c+
    /// dialect output (double-quote quoting, parenthesised ADD/MODIFY, RENAME COLUMN,
    /// DROP INDEX without ON tablename, DROP PRIMARY KEY for PK).
    /// </summary>
    public class OracleTableAlterCommandBuilderTests
    {
        private readonly OracleTableAlterCommandBuilder _builder = new OracleTableAlterCommandBuilder();

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
        [DisplayName("Oracle GetExecutionKind：AddFieldChange 應為 Alter")]
        public void GetExecutionKind_AddField_IsAlter()
        {
            var change = new AddFieldChange(new DbField("col", "Col", FieldDbType.Integer));
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("Oracle GetExecutionKind：RenameFieldChange 應為 Alter")]
        public void GetExecutionKind_Rename_IsAlter()
        {
            var change = new RenameFieldChange("oldname", new DbField("newname", "New", FieldDbType.String));
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("Oracle GetExecutionKind：AlterFieldChange 同 family（Integer→Long）應為 Alter")]
        public void GetExecutionKind_AlterFieldSameFamily_IsAlter()
        {
            var oldField = new DbField("col", "Col", FieldDbType.Integer);
            var newField = new DbField("col", "Col", FieldDbType.Long);
            var change = new AlterFieldChange(oldField, newField);
            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("Oracle GetExecutionKind：AlterFieldChange 跨 family（Integer→String）應為 Rebuild")]
        public void GetExecutionKind_AlterFieldCrossFamily_IsRebuild()
        {
            var oldField = new DbField("col", "Col", FieldDbType.Integer);
            var newField = new DbField("col", "Col", FieldDbType.String) { Length = 50 };
            var change = new AlterFieldChange(oldField, newField);
            Assert.Equal(ChangeExecutionKind.Rebuild, _builder.GetExecutionKind(change));
        }

        // ---------- IsNarrowingChange ----------

        [Fact]
        [DisplayName("Oracle IsNarrowingChange：String 縮短應回傳 true")]
        public void IsNarrowingChange_StringShortened_ReturnsTrue()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            Assert.True(_builder.IsNarrowingChange(new AlterFieldChange(oldField, newField)));
        }

        [Fact]
        [DisplayName("Oracle IsNarrowingChange：String 加長應回傳 false")]
        public void IsNarrowingChange_StringExtended_ReturnsFalse()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            Assert.False(_builder.IsNarrowingChange(new AlterFieldChange(oldField, newField)));
        }

        [Fact]
        [DisplayName("Oracle IsNarrowingChange：Decimal precision 縮減應回傳 true")]
        public void IsNarrowingChange_DecimalPrecisionReduced_ReturnsTrue()
        {
            var oldField = new DbField("amount", "Amount", FieldDbType.Decimal) { Precision = 18, Scale = 4 };
            var newField = new DbField("amount", "Amount", FieldDbType.Decimal) { Precision = 12, Scale = 4 };
            Assert.True(_builder.IsNarrowingChange(new AlterFieldChange(oldField, newField)));
        }

        // ---------- GetStatements ----------

        [Fact]
        [DisplayName("Oracle GetStatements：AddField 產生 ALTER TABLE ADD (...)（雙引號識別符 + 括號）")]
        public void GetStatements_AddField_EmitsAlterTableAdd()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer) { AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AddFieldChange(field));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE \"st_demo\" ADD (", sql);
            Assert.Contains("\"age\" NUMBER(10) DEFAULT 0 NOT NULL", sql);
            Assert.EndsWith(");", sql);
        }

        [Fact]
        [DisplayName("Oracle GetStatements：AlterField 產生 MODIFY (...) 並包含完整 column 定義")]
        public void GetStatements_AlterField_EmitsModify()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = false };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100, AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE \"st_demo\" MODIFY (", sql);
            Assert.Contains("\"name\" VARCHAR2(100 CHAR)", sql);
            Assert.Contains("NOT NULL", sql);
            Assert.EndsWith(");", sql);
        }

        [Fact]
        [DisplayName("Oracle GetStatements：AlterField 不應使用 MODIFY COLUMN 關鍵字（Oracle 是 MODIFY 後直接接 column 定義）")]
        public void GetStatements_AlterField_DoesNotEmitModifyColumn()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            var sql = Assert.Single(statements);
            Assert.DoesNotContain("MODIFY COLUMN", sql);
        }

        [Fact]
        [DisplayName("Oracle GetStatements：RenameField 產生 RENAME COLUMN（雙引號）")]
        public void GetStatements_RenameField_EmitsRenameColumn()
        {
            var change = new RenameFieldChange("oldname", new DbField("newname", "New", FieldDbType.String) { Length = 50 });
            var statements = _builder.GetStatements("st_demo", change);

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE \"st_demo\" RENAME COLUMN \"oldname\" TO \"newname\";", sql);
        }

        [Fact]
        [DisplayName("Oracle GetStatements：AddIndex 非 PK 產生 CREATE INDEX 並帶 ASC")]
        public void GetStatements_AddIndex_NonPk_EmitsCreateIndex()
        {
            var index = BuildIndex("ix_{0}_col", "col", unique: false);
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Contains("CREATE INDEX \"ix_st_demo_col\" ON \"st_demo\"", sql);
            Assert.Contains("\"col\" ASC", sql);
        }

        [Fact]
        [DisplayName("Oracle GetStatements：AddIndex 唯一索引產生 CREATE UNIQUE INDEX")]
        public void GetStatements_AddIndex_Unique_EmitsCreateUniqueIndex()
        {
            var index = BuildIndex("uk_{0}_col", "col", unique: true);
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Contains("CREATE UNIQUE INDEX \"uk_st_demo_col\" ON \"st_demo\"", sql);
        }

        [Fact]
        [DisplayName("Oracle GetStatements：AddIndex PK 產生 ADD CONSTRAINT PRIMARY KEY 且不帶 ASC/DESC")]
        public void GetStatements_AddIndex_Pk_EmitsAddConstraint()
        {
            var pk = BuildPrimaryKey("id");
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(pk));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE \"st_demo\" ADD CONSTRAINT \"pk_st_demo\"", sql);
            Assert.Contains("PRIMARY KEY (\"id\")", sql);
            // Oracle PK constraint 內 column 不接受 ASC/DESC
            Assert.DoesNotContain("PRIMARY KEY (\"id\" ASC", sql);
        }

        [Fact]
        [DisplayName("Oracle GetStatements：DropIndex 非 PK 產生 DROP INDEX（不帶 ON tablename）")]
        public void GetStatements_DropIndex_NonPk_EmitsDropIndex()
        {
            var index = BuildIndex("ix_{0}_col", "col", unique: false);
            // 模擬：當作既有 index name（已 resolve），略過 {0} 替換
            index.Name = "ix_st_demo_col";
            var statements = _builder.GetStatements("st_demo", new DropIndexChange(index));

            var sql = Assert.Single(statements);
            // Oracle DROP INDEX 與 MySQL 不同：不接 ON tablename
            Assert.Equal("DROP INDEX \"ix_st_demo_col\";", sql);
            Assert.DoesNotContain("ON \"st_demo\"", sql);
        }

        [Fact]
        [DisplayName("Oracle GetStatements：DropIndex PK 產生 DROP PRIMARY KEY")]
        public void GetStatements_DropIndex_Pk_EmitsDropPrimaryKey()
        {
            var pk = BuildPrimaryKey("id");
            var statements = _builder.GetStatements("st_demo", new DropIndexChange(pk));

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE \"st_demo\" DROP PRIMARY KEY;", sql);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("Oracle GetStatements：空白 tableName 應拋例外")]
        public void GetStatements_EmptyTableName_Throws(string? tableName)
        {
            var change = new AddFieldChange(new DbField("col", "Col", FieldDbType.Integer));
            Assert.ThrowsAny<ArgumentException>(() => _builder.GetStatements(tableName!, change));
        }
    }
}
