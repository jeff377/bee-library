using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.MySql;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-syntax tests for <see cref="MySqlCreateTableCommandBuilder"/>. No live database
    /// connection — the builder produces string output that is asserted via
    /// <see cref="Assert.Contains(string, string)"/> against the well-known fragments
    /// for MySQL 8.0+ dialect (backtick quoting, BIGINT AUTO_INCREMENT PRIMARY KEY,
    /// utf8mb4_0900_ai_ci CI collation table suffix).
    /// </summary>
    public class MySqlCreateTableCommandBuilderTests
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

        #region GetMySqlType 各 FieldDbType 分支

        [Theory]
        [InlineData(FieldDbType.Boolean, "TINYINT(1)")]
        [InlineData(FieldDbType.Short, "SMALLINT")]
        [InlineData(FieldDbType.Integer, "INT")]
        [InlineData(FieldDbType.Long, "BIGINT")]
        [InlineData(FieldDbType.Currency, "DECIMAL(19,4)")]
        [InlineData(FieldDbType.Date, "DATE")]
        [InlineData(FieldDbType.DateTime, "DATETIME(6)")]
        [InlineData(FieldDbType.Guid, "CHAR(36)")]
        [InlineData(FieldDbType.Binary, "LONGBLOB")]
        [InlineData(FieldDbType.Text, "LONGTEXT")]
        [DisplayName("GetCommandText 應為各 FieldDbType 產生對應的 MySQL 型別字串")]
        public void GetCommandText_FieldDbType_GeneratesCorrectColumnType(FieldDbType dbType, string expectedFragment)
        {
            var schema = BuildSchema(dbType);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains(expectedFragment, sql);
        }

        [Fact]
        [DisplayName("GetCommandText String 型別應使用 VARCHAR 並指定長度")]
        public void GetCommandText_String_UsesVarcharLength()
        {
            var schema = BuildSchema(FieldDbType.String, length: 50);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("VARCHAR(50)", sql);
        }

        [Fact]
        [DisplayName("GetCommandText Decimal 應使用 DECIMAL(precision,scale)")]
        public void GetCommandText_Decimal_UsesDecimal()
        {
            var schema = BuildSchema(FieldDbType.Decimal, precision: 12, scale: 3);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DECIMAL(12,3)", sql);
        }

        [Fact]
        [DisplayName("GetCommandText 不支援的 FieldDbType 應擲出 InvalidOperationException")]
        public void GetCommandText_UnknownDbType_Throws()
        {
            var schema = new TableSchema { TableName = "st_bad" };
            schema.Fields!.Add("col", "Col", (FieldDbType)999);
            var builder = new MySqlCreateTableCommandBuilder();

            Assert.Throws<InvalidOperationException>(() => builder.GetCommandText(schema));
        }

        #endregion

        #region 表後綴與識別符引用

        [Fact]
        [DisplayName("CREATE TABLE 後綴應帶 ENGINE=InnoDB 與 utf8mb4_0900_ai_ci collation（CI 比對 day-1 內建）")]
        public void GetCommandText_TableSuffix_IncludesInnoDbAndCiCollation()
        {
            var schema = BuildSchema(FieldDbType.String, length: 50);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            // ERP CI 比對需求：WHERE name = 'jeff' 應命中 'Jeff'，
            // 由 table-level COLLATE=utf8mb4_0900_ai_ci 統一套用。
            Assert.Contains("ENGINE=InnoDB", sql);
            Assert.Contains("DEFAULT CHARSET=utf8mb4", sql);
            Assert.Contains("COLLATE=utf8mb4_0900_ai_ci", sql);
        }

        [Fact]
        [DisplayName("識別符應以 backtick 引用")]
        public void GetCommandText_Identifiers_UseBackticks()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("CREATE TABLE `st_demo`", sql);
            Assert.Contains("`col`", sql);
        }

        #endregion

        #region Default Expressions

        [Fact]
        [DisplayName("非 AllowNull Integer 欄位應產生 NOT NULL DEFAULT 0")]
        public void GetCommandText_NonNullInteger_GeneratesNotNullDefault0()
        {
            var schema = BuildSchema(FieldDbType.Integer, allowNull: false);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("`col` INT NOT NULL DEFAULT 0", sql);
        }

        [Fact]
        [DisplayName("非 AllowNull String 欄位應產生空字串 DEFAULT")]
        public void GetCommandText_NonNullString_GeneratesEmptyStringDefault()
        {
            var schema = BuildSchema(FieldDbType.String, length: 50, allowNull: false);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT ''", sql);
        }

        [Fact]
        [DisplayName("AllowNull 欄位不應有 DEFAULT 子句")]
        public void GetCommandText_AllowNullField_OmitsDefault()
        {
            var schema = BuildSchema(FieldDbType.Integer, allowNull: true);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("`col` INT NULL", sql);
            Assert.DoesNotContain("`col` INT NULL DEFAULT", sql);
        }

        [Fact]
        [DisplayName("Guid 欄位 DEFAULT 應為 (UUID()) 表達式")]
        public void GetCommandText_NonNullGuid_GeneratesUuidDefault()
        {
            var schema = BuildSchema(FieldDbType.Guid, allowNull: false);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT (UUID())", sql);
        }

        [Fact]
        [DisplayName("DateTime 欄位 DEFAULT 應為 CURRENT_TIMESTAMP(6)")]
        public void GetCommandText_NonNullDateTime_GeneratesCurrentTimestamp()
        {
            var schema = BuildSchema(FieldDbType.DateTime, allowNull: false);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT CURRENT_TIMESTAMP(6)", sql);
        }

        #endregion

        #region AutoIncrement

        [Fact]
        [DisplayName("AutoIncrement 欄位應 inline 為 BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY")]
        public void GetCommandText_AutoIncrement_InlinedOnColumnLine()
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add("sys_no", "No", FieldDbType.AutoIncrement);
            schema.Indexes!.AddPrimaryKey("sys_no");
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("`sys_no` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY", sql);
            // AutoIncrement 採 inline 形式 → 不應再產生外部 CONSTRAINT PRIMARY KEY
            Assert.DoesNotContain("CONSTRAINT", sql);
        }

        [Fact]
        [DisplayName("AutoIncrement 欄位若非單欄 PK 應拋 InvalidOperationException")]
        public void GetCommandText_AutoIncrementNotSinglePk_Throws()
        {
            var schema = new TableSchema { TableName = "st_bad" };
            schema.Fields!.Add("sys_no", "No", FieldDbType.AutoIncrement);
            schema.Fields!.Add("other", "Other", FieldDbType.String, 10);
            schema.Indexes!.AddPrimaryKey("other");

            var builder = new MySqlCreateTableCommandBuilder();
            var ex = Assert.Throws<InvalidOperationException>(() => builder.GetCommandText(schema));
            Assert.Contains("must be the single-column primary key", ex.Message);
        }

        [Fact]
        [DisplayName("無宣告 PK 但有 AutoIncrement 欄位仍應 inline 產生 PRIMARY KEY")]
        public void GetCommandText_AutoIncrementWithoutDeclaredPk_InlinesPrimaryKey()
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add("sys_no", "No", FieldDbType.AutoIncrement);
            // 不在 Indexes 加 PK；由 AutoIncrement 行 inline 提供 PK
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("`sys_no` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY", sql);
            Assert.DoesNotContain("CONSTRAINT", sql);
        }

        [Fact]
        [DisplayName("多個 AutoIncrement 欄位應拋 InvalidOperationException")]
        public void GetCommandText_MultipleAutoIncrement_Throws()
        {
            var schema = new TableSchema { TableName = "st_bad" };
            schema.Fields!.Add("a", "A", FieldDbType.AutoIncrement);
            schema.Fields!.Add("b", "B", FieldDbType.AutoIncrement);
            schema.Indexes!.AddPrimaryKey("a");

            var builder = new MySqlCreateTableCommandBuilder();
            var ex = Assert.Throws<InvalidOperationException>(() => builder.GetCommandText(schema));
            Assert.Contains("at most one AUTO_INCREMENT", ex.Message);
        }

        #endregion

        #region PRIMARY KEY 與索引

        [Fact]
        [DisplayName("無 AutoIncrement 的 schema 應產生獨立 PRIMARY KEY constraint")]
        public void GetCommandText_NonAutoIncrementSchema_EmitsPrimaryKeyConstraint()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            var builder = new MySqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("CONSTRAINT", sql);
            Assert.Contains("PRIMARY KEY", sql);
            Assert.Contains("`sys_rowid`", sql);
        }

        [Fact]
        [DisplayName("非 PK 索引應產生 CREATE INDEX 語句")]
        public void GetCommandText_NonPkIndexes_EmitCreateIndexStatements()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            schema.Indexes!.Add("ix_{0}_col", "col", false);
            schema.Indexes!.Add("uk_{0}_col", "col", true);

            var builder = new MySqlCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            Assert.Contains("CREATE INDEX `ix_st_demo_col` ON `st_demo`", sql);
            Assert.Contains("CREATE UNIQUE INDEX `uk_st_demo_col` ON `st_demo`", sql);
        }

        #endregion
    }
}
