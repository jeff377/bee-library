using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    public class SqliteCreateTableCommandBuilderTests
    {
        private static TableSchema BuildSchema(FieldDbType dbType, int length = 0,
            int precision = 18, int scale = 0, bool allowNull = false, string defaultValue = "")
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            var field = schema.Fields.Add("col", "Col", dbType, length);
            field.Precision = precision;
            field.Scale = scale;
            field.AllowNull = allowNull;
            field.DefaultValue = defaultValue;
            schema.Indexes!.AddPrimaryKey(SysFields.RowId);
            return schema;
        }

        #region GetSqliteType 各 FieldDbType 分支

        [Theory]
        [InlineData(FieldDbType.Boolean, "BOOLEAN")]
        [InlineData(FieldDbType.Short, "SMALLINT")]
        [InlineData(FieldDbType.Integer, "INTEGER")]
        [InlineData(FieldDbType.Long, "BIGINT")]
        [InlineData(FieldDbType.Currency, "NUMERIC(19,4)")]
        [InlineData(FieldDbType.Date, "DATE")]
        [InlineData(FieldDbType.DateTime, "DATETIME")]
        [InlineData(FieldDbType.Guid, "UUID")]
        [InlineData(FieldDbType.Binary, "BLOB")]
        [InlineData(FieldDbType.Text, "TEXT")]
        [DisplayName("GetCommandText 應為各 FieldDbType 產生對應的 SQLite 型別字串")]
        public void GetCommandText_FieldDbType_GeneratesCorrectColumnType(FieldDbType dbType, string expectedFragment)
        {
            var schema = BuildSchema(dbType);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains(expectedFragment, sql);
        }

        [Fact]
        [DisplayName("GetCommandText String 型別應使用 VARCHAR 並指定長度")]
        public void GetCommandText_String_UsesVarcharLength()
        {
            var schema = BuildSchema(FieldDbType.String, length: 50);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("VARCHAR(50)", sql);
        }

        [Fact]
        [DisplayName("GetCommandText Decimal 應使用 NUMERIC(precision,scale)")]
        public void GetCommandText_Decimal_UsesNumeric()
        {
            var schema = BuildSchema(FieldDbType.Decimal, precision: 12, scale: 3);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("NUMERIC(12,3)", sql);
        }

        [Fact]
        [DisplayName("GetCommandText 不支援的 FieldDbType 應擲出 InvalidOperationException")]
        public void GetCommandText_UnknownDbType_Throws()
        {
            var schema = BuildSchema(FieldDbType.Unknown);
            var builder = new SqliteCreateTableCommandBuilder();

            Assert.Throws<InvalidOperationException>(() => builder.GetCommandText(schema));
        }

        #endregion

        #region 結構與分支

        [Fact]
        [DisplayName("GetCommandText 應產生雙引號 quoted 的 CREATE TABLE 語句")]
        public void GetCommandText_New_GeneratesCreateTable()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("-- Create table st_demo", sql);
            Assert.Contains("CREATE TABLE \"st_demo\"", sql);
        }

        [Fact]
        [DisplayName("Guid PK 應產生外部 CONSTRAINT ... PRIMARY KEY 語句")]
        public void GetCommandText_GuidPrimaryKey_GeneratesConstraint()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("CONSTRAINT \"pk_st_demo\" PRIMARY KEY", sql);
            Assert.Contains("\"sys_rowid\"", sql);
        }

        [Fact]
        [DisplayName("含獨立索引時應產生 CREATE INDEX 與 CREATE UNIQUE INDEX")]
        public void GetCommandText_Indexes_GeneratesCreateIndex()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            schema.Indexes!.Add("ix_{0}_col", "col", false);
            schema.Indexes!.Add("uk_{0}_col", "col", true);

            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("CREATE INDEX \"ix_st_demo_col\" ON \"st_demo\"", sql);
            Assert.Contains("CREATE UNIQUE INDEX \"uk_st_demo_col\" ON \"st_demo\"", sql);
        }

        [Fact]
        [DisplayName("AllowNull 欄位應產生 NULL 標記且無 DEFAULT 子句")]
        public void GetCommandText_AllowNull_GeneratesNullWithoutDefault()
        {
            var schema = BuildSchema(FieldDbType.Integer, allowNull: true);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("\"col\" INTEGER NULL", sql);
            Assert.DoesNotContain("\"col\" INTEGER NULL DEFAULT", sql);
        }

        [Fact]
        [DisplayName("非 AllowNull Integer 欄位應產生 NOT NULL DEFAULT 0")]
        public void GetCommandText_NotNullInteger_GeneratesDefaultZero()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("\"col\" INTEGER NOT NULL DEFAULT 0", sql);
        }

        [Fact]
        [DisplayName("String 欄位應產生 '' 預設值")]
        public void GetCommandText_String_GeneratesEmptyStringDefault()
        {
            var schema = BuildSchema(FieldDbType.String, length: 20);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT ''", sql);
        }

        [Fact]
        [DisplayName("自訂 DefaultValue 應寫入 DEFAULT 子句")]
        public void GetCommandText_CustomDefault_AppliedToColumn()
        {
            var schema = BuildSchema(FieldDbType.Integer, defaultValue: "42");
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT 42", sql);
        }

        [Fact]
        [DisplayName("DateTime 欄位應使用 CURRENT_TIMESTAMP 作為預設值")]
        public void GetCommandText_DateTime_DefaultCurrentTimestamp()
        {
            var schema = BuildSchema(FieldDbType.DateTime);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT CURRENT_TIMESTAMP", sql);
        }

        [Fact]
        [DisplayName("Guid 欄位應使用 hex(randomblob(16)) 作為預設值")]
        public void GetCommandText_Guid_DefaultHexRandomblob()
        {
            var schema = BuildSchema(FieldDbType.Guid);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT (hex(randomblob(16)))", sql);
        }

        #endregion

        #region AutoIncrement 內聯 PK + 衝突檢測

        [Fact]
        [DisplayName("AutoIncrement = 單欄 PK 時應內聯 INTEGER PRIMARY KEY AUTOINCREMENT")]
        public void GetCommandText_AutoIncrementAsPrimaryKey_InlinesAutoincrement()
        {
            var schema = new TableSchema { TableName = "st_seq" };
            schema.Fields!.Add(SysFields.No, "Sequence", FieldDbType.AutoIncrement);
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            schema.Indexes!.AddPrimaryKey(SysFields.No);

            var builder = new SqliteCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            Assert.Contains("\"sys_no\" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL", sql);
            Assert.DoesNotContain("CONSTRAINT", sql);
            // The AutoIncrement column line itself must not carry a DEFAULT clause.
            string autoIncrementLine = sql.Split("\r\n").Single(l => l.Contains("\"sys_no\""));
            Assert.DoesNotContain("DEFAULT", autoIncrementLine);
        }

        [Fact]
        [DisplayName("AutoIncrement 欄位但 PK 指向其他欄位應擲 InvalidOperationException")]
        public void GetCommandText_AutoIncrementWithMismatchedPrimaryKey_Throws()
        {
            var schema = new TableSchema { TableName = "st_bad" };
            schema.Fields!.Add(SysFields.No, "Sequence", FieldDbType.AutoIncrement);
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            // PK points to sys_rowid, NOT the AutoIncrement column.
            schema.Indexes!.AddPrimaryKey(SysFields.RowId);

            var builder = new SqliteCreateTableCommandBuilder();
            var ex = Assert.Throws<InvalidOperationException>(() => builder.GetCommandText(schema));
            Assert.Contains("must be the single-column primary key", ex.Message);
        }

        [Fact]
        [DisplayName("AutoIncrement 欄位且無 PK 索引應內聯為 PK（INTEGER PRIMARY KEY AUTOINCREMENT 即為 PK）")]
        public void GetCommandText_AutoIncrementWithoutPrimaryKey_InlinesPrimaryKey()
        {
            var schema = new TableSchema { TableName = "st_seq" };
            schema.Fields!.Add(SysFields.No, "Sequence", FieldDbType.AutoIncrement);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 20);
            // No primary key index declared — the inlined AUTOINCREMENT column is the PK.

            var builder = new SqliteCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            Assert.Contains("\"sys_no\" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL", sql);
            Assert.DoesNotContain("CONSTRAINT", sql);
        }

        [Fact]
        [DisplayName("多個 AutoIncrement 欄位應擲 InvalidOperationException")]
        public void GetCommandText_MultipleAutoIncrementFields_Throws()
        {
            var schema = new TableSchema { TableName = "st_bad" };
            schema.Fields!.Add("a", "A", FieldDbType.AutoIncrement);
            schema.Fields!.Add("b", "B", FieldDbType.AutoIncrement);
            schema.Indexes!.AddPrimaryKey("a");

            var builder = new SqliteCreateTableCommandBuilder();
            var ex = Assert.Throws<InvalidOperationException>(() => builder.GetCommandText(schema));
            Assert.Contains("at most one AUTOINCREMENT", ex.Message);
        }

        #endregion

        #region COMMENT no-op

        [Fact]
        [DisplayName("GetCommandText 不應產生任何 COMMENT 語句（SQLite 不持久化描述）")]
        public void GetCommandText_NeverEmitsCommentStatements()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            schema.DisplayName = "示範資料表";
            schema.Fields!["col"].Caption = "數值欄位";
            schema.Fields![SysFields.RowId].Caption = "唯一識別";

            var builder = new SqliteCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            Assert.DoesNotContain("COMMENT", sql, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region COLLATE NOCASE for case-insensitive compare

        [Theory]
        [InlineData(FieldDbType.String, 50)]
        [InlineData(FieldDbType.Text, 0)]
        [DisplayName("GetCommandText 文字欄位（String/Text）column 定義應帶 COLLATE NOCASE")]
        public void GetCommandText_TextField_IncludesCollateNocase(FieldDbType dbType, int length)
        {
            var schema = BuildSchema(dbType, length: length);
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            // ERP CI 比對需求：WHERE name = 'jeff' 應命中 'Jeff'，
            // 由 column 級 COLLATE NOCASE 套用實現。
            Assert.Contains("COLLATE NOCASE", sql);
        }

        [Fact]
        [DisplayName("GetCommandText 全為非文字欄位的 schema 不應出現 COLLATE 子句")]
        public void GetCommandText_NonTextSchema_OmitsCollate()
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add("rowid", "Row ID", FieldDbType.Guid);
            schema.Fields.Add("count", "Count", FieldDbType.Integer);
            schema.Fields.Add("amount", "Amount", FieldDbType.Decimal);
            schema.Fields.Add("created", "Created", FieldDbType.DateTime);
            schema.Fields.Add("data", "Data", FieldDbType.Binary);
            schema.Indexes!.AddPrimaryKey("rowid");
            var builder = new SqliteCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.DoesNotContain("COLLATE", sql);
        }

        #endregion

        #region 複合 PK / 複合索引

        [Fact]
        [DisplayName("GetCommandText 複合 PK 應產生逗號分隔的 PRIMARY KEY 欄位列")]
        public void GetCommandText_CompositePrimaryKey_EmitsCommaSeparatedFields()
        {
            var schema = new TableSchema { TableName = "st_compo" };
            schema.Fields!.Add("a", "A", FieldDbType.Integer);
            schema.Fields.Add("b", "B", FieldDbType.Integer);
            schema.Indexes!.AddPrimaryKey("a,b");

            var builder = new SqliteCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            Assert.Contains("CONSTRAINT \"pk_st_compo\" PRIMARY KEY (\"a\" ASC, \"b\" ASC)", sql);
        }

        [Fact]
        [DisplayName("GetCommandText 複合二級索引應產生逗號分隔的 INDEX 欄位列")]
        public void GetCommandText_CompositeSecondaryIndex_EmitsCommaSeparatedFields()
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            schema.Fields.Add("a", "A", FieldDbType.Integer);
            schema.Fields.Add("b", "B", FieldDbType.Integer);
            schema.Indexes!.AddPrimaryKey(SysFields.RowId);
            schema.Indexes.Add("ix_{0}_a_b", "a,b", false);

            var builder = new SqliteCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            Assert.Contains("CREATE INDEX \"ix_st_demo_a_b\" ON \"st_demo\" (\"a\" ASC, \"b\" ASC);", sql);
        }

        #endregion
    }
}
