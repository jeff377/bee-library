using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    public class SqlCreateTableCommandBuilderTests
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

        #region ConverDbType 各 FieldDbType 分支

        [Theory]
        [InlineData(FieldDbType.Boolean, "[bit]")]
        [InlineData(FieldDbType.AutoIncrement, "[int] IDENTITY(1,1)")]
        [InlineData(FieldDbType.Short, "[smallint]")]
        [InlineData(FieldDbType.Integer, "[int]")]
        [InlineData(FieldDbType.Long, "[bigint]")]
        [InlineData(FieldDbType.Currency, "[decimal](19,4)")]
        [InlineData(FieldDbType.Date, "[date]")]
        [InlineData(FieldDbType.DateTime, "[datetime]")]
        [InlineData(FieldDbType.Guid, "[uniqueidentifier]")]
        [InlineData(FieldDbType.Binary, "[varbinary](max)")]
        [InlineData(FieldDbType.Text, "[nvarchar](max)")]
        [DisplayName("GetCommandText 應為各 FieldDbType 產生對應的 SQL Server 型別字串")]
        public void GetCommandText_FieldDbType_GeneratesCorrectColumnType(FieldDbType dbType, string expectedFragment)
        {
            var schema = BuildSchema(dbType);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains(expectedFragment, sql);
        }

        [Fact]
        [DisplayName("GetCommandText String 型別應使用指定的長度")]
        public void GetCommandText_String_UsesLength()
        {
            var schema = BuildSchema(FieldDbType.String, length: 50);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("[nvarchar](50)", sql);
        }

        [Fact]
        [DisplayName("GetCommandText Decimal 應使用指定的 Precision/Scale")]
        public void GetCommandText_Decimal_UsesPrecisionAndScale()
        {
            var schema = BuildSchema(FieldDbType.Decimal, precision: 12, scale: 3);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("[decimal](12,3)", sql);
        }

        [Fact]
        [DisplayName("GetCommandText 不支援的 FieldDbType 應擲出 InvalidOperationException")]
        public void GetCommandText_UnknownDbType_Throws()
        {
            var schema = BuildSchema(FieldDbType.Unknown);
            var builder = new SqlCreateTableCommandBuilder();

            Assert.Throws<InvalidOperationException>(() => builder.GetCommandText(schema));
        }

        #endregion

        #region 結構與分支

        [Fact]
        [DisplayName("GetCommandText UpgradeAction.New 應產生 CREATE TABLE 語句")]
        public void GetCommandText_New_GeneratesCreateTable()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            schema.UpgradeAction = DbUpgradeAction.New;
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("-- Create table st_demo", sql);
            Assert.Contains("CREATE TABLE [st_demo]", sql);
        }

        [Fact]
        [DisplayName("含 PrimaryKey 索引時應產生 CONSTRAINT ... PRIMARY KEY 語句")]
        public void GetCommandText_PrimaryKey_GeneratesConstraint()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("CONSTRAINT [pk_st_demo] PRIMARY KEY", sql);
            Assert.Contains("[sys_rowid]", sql);
        }

        [Fact]
        [DisplayName("含獨立索引時應產生 CREATE INDEX 與 CREATE UNIQUE INDEX")]
        public void GetCommandText_Indexes_GeneratesCreateIndex()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            schema.Indexes!.Add("ix_{0}_col", "col", false);
            schema.Indexes!.Add("uk_{0}_col", "col", true);

            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("CREATE INDEX [ix_st_demo_col] ON [st_demo]", sql);
            Assert.Contains("CREATE UNIQUE INDEX [uk_st_demo_col] ON [st_demo]", sql);
        }

        [Fact]
        [DisplayName("AllowNull 欄位應產生 NULL 標記且無 DEFAULT 子句")]
        public void GetCommandText_AllowNull_GeneratesNullWithoutDefault()
        {
            var schema = BuildSchema(FieldDbType.Integer, allowNull: true);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("[col] [int] NULL", sql);
            Assert.DoesNotContain("[col] [int] NULL DEFAULT", sql);
        }

        [Fact]
        [DisplayName("非 AllowNull Integer 欄位應產生 NOT NULL DEFAULT (0)")]
        public void GetCommandText_NotNullInteger_GeneratesDefaultZero()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("[col] [int] NOT NULL DEFAULT (0)", sql);
        }

        [Fact]
        [DisplayName("String 欄位應產生 N'...' 預設值")]
        public void GetCommandText_String_GeneratesNStringDefault()
        {
            var schema = BuildSchema(FieldDbType.String, length: 20);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT (N'')", sql);
        }

        [Fact]
        [DisplayName("自訂 DefaultValue 應寫入 DEFAULT 子句")]
        public void GetCommandText_CustomDefault_AppliedToColumn()
        {
            var schema = BuildSchema(FieldDbType.Integer, defaultValue: "42");
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT (42)", sql);
        }

        [Fact]
        [DisplayName("DateTime 欄位應使用 getdate() 作為預設值")]
        public void GetCommandText_DateTime_DefaultGetdate()
        {
            var schema = BuildSchema(FieldDbType.DateTime);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT (getdate())", sql);
        }

        [Fact]
        [DisplayName("Guid 欄位應使用 newid() 作為預設值")]
        public void GetCommandText_Guid_DefaultNewid()
        {
            var schema = BuildSchema(FieldDbType.Guid);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("DEFAULT (newid())", sql);
        }

        [Fact]
        [DisplayName("AutoIncrement 欄位不應產生 DEFAULT 子句")]
        public void GetCommandText_AutoIncrement_NoDefault()
        {
            var schema = BuildSchema(FieldDbType.AutoIncrement);
            var builder = new SqlCreateTableCommandBuilder();

            string sql = builder.GetCommandText(schema);

            Assert.Contains("[col] [int] IDENTITY(1,1) NOT NULL", sql);
            Assert.DoesNotContain("[col] [int] IDENTITY(1,1) NOT NULL DEFAULT", sql);
        }

        #endregion

        #region Extended property (description) 產出

        [Fact]
        [DisplayName("GetCommandText 有 DisplayName 與 Caption 時應產生 sp_addextendedproperty")]
        public void GetCommandText_WithDisplayNameAndCaption_IncludesExtendedProperty()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            schema.DisplayName = "示範資料表";
            schema.Fields!["col"].Caption = "數值欄位";

            var builder = new SqlCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            // 表層
            Assert.Contains("EXEC sp_addextendedproperty", sql);
            Assert.Contains("@value=N'示範資料表'", sql);
            Assert.Contains("@level1type=N'TABLE', @level1name=N'st_demo'", sql);
            // 欄位層
            Assert.Contains("@value=N'數值欄位'", sql);
            Assert.Contains("@level2type=N'COLUMN', @level2name=N'col'", sql);
        }

        [Fact]
        [DisplayName("GetCommandText DisplayName 為空時不產生表層 sp_addextendedproperty")]
        public void GetCommandText_WithEmptyDisplayName_OmitsTableExtendedProperty()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            // DisplayName 預設即為空，只設欄位 Caption
            schema.Fields!["col"].Caption = "數值欄位";

            var builder = new SqlCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            // 不應有表層 extended property（無 level2 的那條）
            Assert.DoesNotContain("@level1type=N'TABLE', @level1name=N'st_demo';", sql);
            // 但欄位層仍應產出
            Assert.Contains("@level2type=N'COLUMN', @level2name=N'col'", sql);
        }

        [Fact]
        [DisplayName("GetCommandText Caption 為空的欄位不產生欄位層 sp_addextendedproperty")]
        public void GetCommandText_WithEmptyCaption_OmitsColumnExtendedProperty()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            schema.DisplayName = "示範資料表";
            // 把欄位 Caption 清空
            schema.Fields!["col"].Caption = string.Empty;
            schema.Fields![SysFields.RowId].Caption = string.Empty;

            var builder = new SqlCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            // 表層有
            Assert.Contains("@value=N'示範資料表'", sql);
            // 欄位層無
            Assert.DoesNotContain("@level2type=N'COLUMN'", sql);
        }

        [Fact]
        [DisplayName("GetCommandText 含單引號時應正確 escape 為雙單引號")]
        public void GetCommandText_WithSingleQuote_EscapesCorrectly()
        {
            var schema = BuildSchema(FieldDbType.Integer);
            schema.DisplayName = "O'Brien 表";
            schema.Fields!["col"].Caption = "it's a field";

            var builder = new SqlCreateTableCommandBuilder();
            string sql = builder.GetCommandText(schema);

            Assert.Contains("@value=N'O''Brien 表'", sql);
            Assert.Contains("@value=N'it''s a field'", sql);
        }

        #endregion
    }
}
