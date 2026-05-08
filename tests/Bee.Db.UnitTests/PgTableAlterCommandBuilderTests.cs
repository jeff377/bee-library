using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.PostgreSql;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class PgTableAlterCommandBuilderTests
    {
        private readonly PgTableAlterCommandBuilder _builder = new();

        // ---------- GetExecutionKind ----------

        [Fact]
        [DisplayName("PG GetExecutionKind：AddFieldChange 應為 Alter")]
        public void GetExecutionKind_AddField_ReturnsAlter()
        {
            var change = new AddFieldChange(new DbField("age", "Age", FieldDbType.Integer));

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("PG GetExecutionKind：AlterFieldChange 同 family 應為 Alter")]
        public void GetExecutionKind_AlterFieldSameFamily_ReturnsAlter()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var change = new AlterFieldChange(oldField, newField);

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("PG GetExecutionKind：AlterFieldChange 跨 family 應為 Rebuild")]
        public void GetExecutionKind_AlterFieldCrossFamily_ReturnsRebuild()
        {
            var oldField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            var newField = new DbField("v", "V", FieldDbType.Integer);
            var change = new AlterFieldChange(oldField, newField);

            Assert.Equal(ChangeExecutionKind.Rebuild, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("PG GetExecutionKind：AlterFieldChange AutoIncrement 切換應為 Rebuild")]
        public void GetExecutionKind_AlterFieldAutoIncrementToggle_ReturnsRebuild()
        {
            var oldField = new DbField("id", "Id", FieldDbType.Integer);
            var newField = new DbField("id", "Id", FieldDbType.AutoIncrement);
            var change = new AlterFieldChange(oldField, newField);

            Assert.Equal(ChangeExecutionKind.Rebuild, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("PG GetExecutionKind：AddIndexChange 應為 Alter")]
        public void GetExecutionKind_AddIndex_ReturnsAlter()
        {
            var index = new TableSchemaIndex { Name = "ix_demo_name" };
            index.IndexFields!.Add("name");

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(new AddIndexChange(index)));
        }

        [Fact]
        [DisplayName("PG GetExecutionKind：DropIndexChange 應為 Alter")]
        public void GetExecutionKind_DropIndex_ReturnsAlter()
        {
            var index = new TableSchemaIndex { Name = "ix_demo_name" };
            index.IndexFields!.Add("name");

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(new DropIndexChange(index)));
        }

        // ---------- IsNarrowingChange ----------

        [Fact]
        [DisplayName("PG IsNarrowingChange：非 AlterField 應回傳 false")]
        public void IsNarrowingChange_NonAlterChange_ReturnsFalse()
        {
            var change = new AddFieldChange(new DbField("age", "Age", FieldDbType.Integer));

            Assert.False(_builder.IsNarrowingChange(change));
        }

        [Fact]
        [DisplayName("PG IsNarrowingChange：String 縮短應回傳 true")]
        public void IsNarrowingChange_StringShortened_ReturnsTrue()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };

            Assert.True(_builder.IsNarrowingChange(new AlterFieldChange(oldField, newField)));
        }

        // ---------- AddField statements ----------

        [Fact]
        [DisplayName("PG GetStatements：AddField 產生 ALTER TABLE ADD COLUMN 並含 DEFAULT")]
        public void GetStatements_AddField_EmitsAlterTableAddColumn()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer) { AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AddFieldChange(field));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE \"st_demo\" ADD COLUMN", sql);
            Assert.Contains("\"age\" integer NOT NULL", sql);
            Assert.Contains("DEFAULT", sql);
        }

        [Fact]
        [DisplayName("PG GetStatements：AddField nullable 欄位不產生 DEFAULT")]
        public void GetStatements_AddFieldNullable_NoDefault()
        {
            var field = new DbField("note", "Note", FieldDbType.String) { Length = 100, AllowNull = true };
            var statements = _builder.GetStatements("st_demo", new AddFieldChange(field));

            var sql = Assert.Single(statements);
            Assert.Contains("\"note\" varchar(100) NULL", sql);
            Assert.DoesNotContain("DEFAULT", sql);
        }

        // ---------- AlterField statements ----------

        [Fact]
        [DisplayName("PG GetStatements：AlterField 僅長度變更應產生 ALTER COLUMN TYPE 一段")]
        public void GetStatements_AlterFieldLengthChanged_EmitsAlterColumnType()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = false };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100, AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE \"st_demo\" ALTER COLUMN \"name\" TYPE varchar(100);", sql);
        }

        [Fact]
        [DisplayName("PG GetStatements：AlterField NOT NULL → NULL 應產生 DROP NOT NULL + DROP DEFAULT")]
        public void GetStatements_AlterFieldToNullable_DropsNotNullAndDefault()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = false };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = true };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            Assert.Equal(2, statements.Count);
            Assert.Equal("ALTER TABLE \"st_demo\" ALTER COLUMN \"name\" DROP NOT NULL;", statements[0]);
            Assert.Equal("ALTER TABLE \"st_demo\" ALTER COLUMN \"name\" DROP DEFAULT;", statements[1]);
        }

        [Fact]
        [DisplayName("PG GetStatements：AlterField 僅 default 變更應產生 SET DEFAULT 一段")]
        public void GetStatements_AlterFieldDefaultOnly_EmitsSetDefault()
        {
            var oldField = new DbField("code", "Code", FieldDbType.String) { Length = 10, AllowNull = false, DefaultValue = "A" };
            var newField = new DbField("code", "Code", FieldDbType.String) { Length = 10, AllowNull = false, DefaultValue = "B" };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE \"st_demo\" ALTER COLUMN \"code\" SET DEFAULT 'B';", sql);
        }

        // ---------- AddIndex statements ----------

        [Fact]
        [DisplayName("PG GetStatements：AddIndex 非唯一索引應產生 CREATE INDEX")]
        public void GetStatements_AddRegularIndex_EmitsCreateIndex()
        {
            var index = new TableSchemaIndex { Name = "ix_{0}_name" };
            index.IndexFields!.Add("name");
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("CREATE INDEX \"ix_st_demo_name\" ON \"st_demo\" (\"name\" ASC);", sql);
        }

        [Fact]
        [DisplayName("PG GetStatements：AddIndex 唯一索引應含 UNIQUE")]
        public void GetStatements_AddUniqueIndex_EmitsUniqueClause()
        {
            var index = new TableSchemaIndex { Name = "ix_{0}_name", Unique = true };
            index.IndexFields!.Add("name");
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Contains("CREATE UNIQUE INDEX", sql);
        }

        [Fact]
        [DisplayName("PG GetStatements：AddIndex 主鍵應產生 ALTER TABLE ADD CONSTRAINT PRIMARY KEY")]
        public void GetStatements_AddPrimaryKey_EmitsAddConstraintPrimaryKey()
        {
            var index = new TableSchemaIndex { Name = "pk_{0}", PrimaryKey = true, Unique = true };
            index.IndexFields!.Add("id");
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE \"st_demo\" ADD CONSTRAINT \"pk_st_demo\" PRIMARY KEY (\"id\" ASC);", sql);
        }

        // ---------- DropIndex statements ----------

        [Fact]
        [DisplayName("PG GetStatements：DropIndex 非主鍵應產生 DROP INDEX")]
        public void GetStatements_DropRegularIndex_EmitsDropIndex()
        {
            var index = new TableSchemaIndex { Name = "ix_st_demo_name" };
            index.IndexFields!.Add("name");
            var statements = _builder.GetStatements("st_demo", new DropIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("DROP INDEX \"ix_st_demo_name\";", sql);
        }

        [Fact]
        [DisplayName("PG GetStatements：DropIndex 主鍵應產生 ALTER TABLE DROP CONSTRAINT")]
        public void GetStatements_DropPrimaryKey_EmitsDropConstraint()
        {
            var index = new TableSchemaIndex { Name = "pk_st_demo", PrimaryKey = true };
            index.IndexFields!.Add("id");
            var statements = _builder.GetStatements("st_demo", new DropIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE \"st_demo\" DROP CONSTRAINT \"pk_st_demo\";", sql);
        }

        // ---------- Edge cases ----------

        [Fact]
        [DisplayName("PG GetStatements：tableName 為 null 應 throw")]
        public void GetStatements_NullTableName_Throws()
        {
            var change = new AddFieldChange(new DbField("a", "A", FieldDbType.Integer));

            Assert.ThrowsAny<ArgumentException>(() => _builder.GetStatements(null!, change));
        }

        // ---------- RenameFieldChange ----------

        [Fact]
        [DisplayName("PG GetExecutionKind：RenameFieldChange 應為 Alter")]
        public void GetExecutionKind_RenameField_ReturnsAlter()
        {
            var change = new RenameFieldChange("emp_name", new DbField("employee_name", "Name", FieldDbType.String) { Length = 50 });

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("PG IsNarrowingChange：RenameFieldChange 應回傳 false")]
        public void IsNarrowingChange_RenameField_ReturnsFalse()
        {
            var change = new RenameFieldChange("emp_name", new DbField("employee_name", "Name", FieldDbType.String) { Length = 50 });

            Assert.False(_builder.IsNarrowingChange(change));
        }

        [Fact]
        [DisplayName("PG GetStatements：RenameFieldChange 應產生 ALTER TABLE RENAME COLUMN 語句")]
        public void GetStatements_RenameField_EmitsRenameColumn()
        {
            var change = new RenameFieldChange("emp_name", new DbField("employee_name", "Name", FieldDbType.String) { Length = 50 });
            var statements = _builder.GetStatements("st_demo", change);

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE \"st_demo\" RENAME COLUMN \"emp_name\" TO \"employee_name\";", sql);
        }
    }
}
