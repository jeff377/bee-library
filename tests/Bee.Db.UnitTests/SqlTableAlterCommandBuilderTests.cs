using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqlTableAlterCommandBuilderTests
    {
        private readonly SqlTableAlterCommandBuilder _builder = new();

        // ---------- GetExecutionKind ----------

        [Fact]
        [DisplayName("GetExecutionKind：AddFieldChange 應為 Alter")]
        public void GetExecutionKind_AddField_ReturnsAlter()
        {
            var change = new AddFieldChange(new DbField("age", "Age", FieldDbType.Integer));

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("GetExecutionKind：AlterFieldChange 同 family 應為 Alter")]
        public void GetExecutionKind_AlterFieldSameFamily_ReturnsAlter()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var change = new AlterFieldChange(oldField, newField);

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("GetExecutionKind：AlterFieldChange 跨 family 應為 Rebuild")]
        public void GetExecutionKind_AlterFieldCrossFamily_ReturnsRebuild()
        {
            var oldField = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            var newField = new DbField("v", "V", FieldDbType.Integer);
            var change = new AlterFieldChange(oldField, newField);

            Assert.Equal(ChangeExecutionKind.Rebuild, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("GetExecutionKind：AlterFieldChange AutoIncrement 切換應為 Rebuild")]
        public void GetExecutionKind_AlterFieldAutoIncrementToggle_ReturnsRebuild()
        {
            var oldField = new DbField("id", "Id", FieldDbType.Integer);
            var newField = new DbField("id", "Id", FieldDbType.AutoIncrement);
            var change = new AlterFieldChange(oldField, newField);

            Assert.Equal(ChangeExecutionKind.Rebuild, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("GetExecutionKind：AddIndexChange 應為 Alter")]
        public void GetExecutionKind_AddIndex_ReturnsAlter()
        {
            var index = new TableSchemaIndex { Name = "ix_demo_name" };
            index.IndexFields!.Add("name");

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(new AddIndexChange(index)));
        }

        [Fact]
        [DisplayName("GetExecutionKind：DropIndexChange 應為 Alter")]
        public void GetExecutionKind_DropIndex_ReturnsAlter()
        {
            var index = new TableSchemaIndex { Name = "ix_demo_name" };
            index.IndexFields!.Add("name");

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(new DropIndexChange(index)));
        }

        // ---------- IsNarrowingChange ----------

        [Fact]
        [DisplayName("IsNarrowingChange：非 AlterField 應回傳 false")]
        public void IsNarrowingChange_NonAlterChange_ReturnsFalse()
        {
            var change = new AddFieldChange(new DbField("age", "Age", FieldDbType.Integer));

            Assert.False(_builder.IsNarrowingChange(change));
        }

        [Fact]
        [DisplayName("IsNarrowingChange：String 縮短應回傳 true")]
        public void IsNarrowingChange_StringShortened_ReturnsTrue()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 100 };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50 };

            Assert.True(_builder.IsNarrowingChange(new AlterFieldChange(oldField, newField)));
        }

        // ---------- AddField statements ----------

        [Fact]
        [DisplayName("GetStatements：AddField 產生 ALTER TABLE ADD 並含 DEFAULT")]
        public void GetStatements_AddField_EmitsAlterTableAdd()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer) { AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AddFieldChange(field));

            var sql = Assert.Single(statements);
            Assert.Contains("ALTER TABLE [st_demo] ADD", sql);
            Assert.Contains("[age] [int] NOT NULL", sql);
            Assert.Contains("DEFAULT", sql);
        }

        [Fact]
        [DisplayName("GetStatements：AddField nullable 欄位不產生 DEFAULT")]
        public void GetStatements_AddFieldNullable_NoDefault()
        {
            var field = new DbField("note", "Note", FieldDbType.String) { Length = 100, AllowNull = true };
            var statements = _builder.GetStatements("st_demo", new AddFieldChange(field));

            var sql = Assert.Single(statements);
            Assert.Contains("[note] [nvarchar](100) NULL", sql);
            Assert.DoesNotContain("DEFAULT", sql);
        }

        // ---------- AlterField statements ----------

        [Fact]
        [DisplayName("GetStatements：AlterField 僅長度變更應產生 drop-default + ALTER COLUMN + add-default 三段")]
        public void GetStatements_AlterFieldLengthChanged_EmitsThreeStatements()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = false };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 100, AllowNull = false };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            Assert.Equal(3, statements.Count);
            Assert.Contains("DROP CONSTRAINT", statements[0]);
            Assert.Contains("ALTER COLUMN [name] [nvarchar](100) NOT NULL", statements[1]);
            Assert.Contains("ADD CONSTRAINT [DF_st_demo_name] DEFAULT", statements[2]);
        }

        [Fact]
        [DisplayName("GetStatements：AlterField NOT NULL → NULL 應執行 ALTER COLUMN 且不新增 default")]
        public void GetStatements_AlterFieldToNullable_NoAddDefault()
        {
            var oldField = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = false };
            var newField = new DbField("name", "Name", FieldDbType.String) { Length = 50, AllowNull = true };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            Assert.Equal(2, statements.Count);
            Assert.Contains("DROP CONSTRAINT", statements[0]);
            Assert.Contains("ALTER COLUMN [name] [nvarchar](50) NULL", statements[1]);
            Assert.DoesNotContain(statements, s => s.Contains("ADD CONSTRAINT") && s.Contains("DEFAULT"));
        }

        [Fact]
        [DisplayName("GetStatements：AlterField 僅 default 變更應不含 ALTER COLUMN")]
        public void GetStatements_AlterFieldDefaultOnly_NoAlterColumn()
        {
            var oldField = new DbField("code", "Code", FieldDbType.String) { Length = 10, AllowNull = false, DefaultValue = "A" };
            var newField = new DbField("code", "Code", FieldDbType.String) { Length = 10, AllowNull = false, DefaultValue = "B" };
            var statements = _builder.GetStatements("st_demo", new AlterFieldChange(oldField, newField));

            Assert.Equal(2, statements.Count);
            Assert.Contains("DROP CONSTRAINT", statements[0]);
            Assert.DoesNotContain(statements, s => s.Contains("ALTER COLUMN"));
            Assert.Contains("ADD CONSTRAINT [DF_st_demo_code] DEFAULT", statements[1]);
        }

        // ---------- AddIndex statements ----------

        [Fact]
        [DisplayName("GetStatements：AddIndex 非唯一索引應產生 CREATE INDEX")]
        public void GetStatements_AddRegularIndex_EmitsCreateIndex()
        {
            var index = new TableSchemaIndex { Name = "ix_{0}_name" };
            index.IndexFields!.Add("name");
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("CREATE INDEX [ix_st_demo_name] ON [st_demo] ([name] ASC);", sql);
        }

        [Fact]
        [DisplayName("GetStatements：AddIndex 唯一索引應含 UNIQUE")]
        public void GetStatements_AddUniqueIndex_EmitsUniqueClause()
        {
            var index = new TableSchemaIndex { Name = "ix_{0}_name", Unique = true };
            index.IndexFields!.Add("name");
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Contains("CREATE UNIQUE INDEX", sql);
        }

        [Fact]
        [DisplayName("GetStatements：AddIndex 主鍵應產生 ALTER TABLE ADD CONSTRAINT PRIMARY KEY")]
        public void GetStatements_AddPrimaryKey_EmitsAddConstraintPrimaryKey()
        {
            var index = new TableSchemaIndex { Name = "pk_{0}", PrimaryKey = true, Unique = true };
            index.IndexFields!.Add("id");
            var statements = _builder.GetStatements("st_demo", new AddIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE [st_demo] ADD CONSTRAINT [pk_st_demo] PRIMARY KEY ([id] ASC);", sql);
        }

        // ---------- DropIndex statements ----------

        [Fact]
        [DisplayName("GetStatements：DropIndex 非主鍵應產生 DROP INDEX")]
        public void GetStatements_DropRegularIndex_EmitsDropIndex()
        {
            var index = new TableSchemaIndex { Name = "ix_st_demo_name" };
            index.IndexFields!.Add("name");
            var statements = _builder.GetStatements("st_demo", new DropIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("DROP INDEX [ix_st_demo_name] ON [st_demo];", sql);
        }

        [Fact]
        [DisplayName("GetStatements：DropIndex 主鍵應產生 ALTER TABLE DROP CONSTRAINT")]
        public void GetStatements_DropPrimaryKey_EmitsDropConstraint()
        {
            var index = new TableSchemaIndex { Name = "pk_st_demo", PrimaryKey = true };
            index.IndexFields!.Add("id");
            var statements = _builder.GetStatements("st_demo", new DropIndexChange(index));

            var sql = Assert.Single(statements);
            Assert.Equal("ALTER TABLE [st_demo] DROP CONSTRAINT [pk_st_demo];", sql);
        }

        // ---------- Edge cases ----------

        [Fact]
        [DisplayName("GetStatements：tableName 為 null 應 throw")]
        public void GetStatements_NullTableName_Throws()
        {
            var change = new AddFieldChange(new DbField("a", "A", FieldDbType.Integer));

            Assert.ThrowsAny<ArgumentException>(() => _builder.GetStatements(null!, change));
        }

        // ---------- RenameFieldChange ----------

        [Fact]
        [DisplayName("GetExecutionKind：RenameFieldChange 應為 Alter")]
        public void GetExecutionKind_RenameField_ReturnsAlter()
        {
            var change = new RenameFieldChange("emp_name", new DbField("employee_name", "Name", FieldDbType.String) { Length = 50 });

            Assert.Equal(ChangeExecutionKind.Alter, _builder.GetExecutionKind(change));
        }

        [Fact]
        [DisplayName("IsNarrowingChange：RenameFieldChange 應回傳 false")]
        public void IsNarrowingChange_RenameField_ReturnsFalse()
        {
            var change = new RenameFieldChange("emp_name", new DbField("employee_name", "Name", FieldDbType.String) { Length = 50 });

            Assert.False(_builder.IsNarrowingChange(change));
        }

        [Fact]
        [DisplayName("GetStatements：RenameFieldChange 應產生 sp_rename 語句")]
        public void GetStatements_RenameField_EmitsSpRename()
        {
            var change = new RenameFieldChange("emp_name", new DbField("employee_name", "Name", FieldDbType.String) { Length = 50 });
            var statements = _builder.GetStatements("st_demo", change);

            var sql = Assert.Single(statements);
            Assert.Equal("EXEC sp_rename N'st_demo.emp_name', N'employee_name', N'COLUMN';", sql);
        }
    }
}
