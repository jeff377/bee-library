using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-syntax tests for <see cref="OracleFormCommandBuilder"/>. Verifies that the
    /// Oracle provider routes through the dialect-agnostic cores in <see cref="Bee.Db.Dml"/>
    /// with <see cref="Bee.Definition.Database.DatabaseType.Oracle"/> and emits double-quoted
    /// identifiers + <c>:</c> bind-variable prefixes (per <see cref="DbFunc.QuoteIdentifier"/>
    /// and <see cref="DbFunc.GetDbParameterPrefix"/>).
    /// </summary>
    [Collection("Initialize")]
    public class OracleFormCommandBuilderTests
    {
        private static FormSchema BuildFooSchema()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            table.DbTableName = "tb_foo";
            table.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            table.Fields!.AddStringField("name", "Name", 50);
            return schema;
        }

        [Fact]
        [DisplayName("FormSchema 建構子 null 應擲 ArgumentNullException")]
        public void Constructor_NullFormSchema_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new OracleFormCommandBuilder((FormSchema)null!));
        }

        [Fact]
        [DisplayName("ProgID 建構子找不到 FormSchema 檔案應擲例外")]
        public void Constructor_UnknownProgId_Throws()
        {
            Assert.Throws<System.IO.FileNotFoundException>(() => new OracleFormCommandBuilder("__not_exists__"));
        }

        [Fact]
        [DisplayName("BuildSelect 應委派至 Oracle 方言並產生 SELECT 語句（雙引號識別符）")]
        public void BuildSelect_DelegatesToOracleDialect()
        {
            var builder = new OracleFormCommandBuilder(BuildFooSchema());

            var spec = builder.BuildSelect("Foo", "name", null, null);

            Assert.Contains("\"TB_FOO\"", spec.CommandText);
            Assert.Contains("\"NAME\"", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildInsert 應委派至 Oracle 方言並產生 INSERT 語句")]
        public void BuildInsert_DelegatesToOracleDialect()
        {
            var dt = new DataTable();
            dt.Columns.Add("name", typeof(string));
            var row = dt.NewRow();
            row["name"] = "n";

            var builder = new OracleFormCommandBuilder(BuildFooSchema());
            var spec = builder.BuildInsert("Foo", row);

            Assert.Contains("INSERT INTO \"TB_FOO\"", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildUpdate 應委派至 Oracle 方言並產生 UPDATE 語句")]
        public void BuildUpdate_DelegatesToOracleDialect()
        {
            var dt = new DataTable();
            dt.Columns.Add(SysFields.RowId, typeof(Guid));
            dt.Columns.Add("name", typeof(string));
            var row = dt.NewRow();
            row[SysFields.RowId] = Guid.NewGuid();
            row["name"] = "old";
            dt.Rows.Add(row);
            dt.AcceptChanges();
            row["name"] = "new";

            var builder = new OracleFormCommandBuilder(BuildFooSchema());
            var spec = builder.BuildUpdate("Foo", row);

            Assert.Contains("UPDATE \"TB_FOO\"", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildDelete 應委派至 Oracle 方言並產生 DELETE 語句")]
        public void BuildDelete_DelegatesToOracleDialect()
        {
            var builder = new OracleFormCommandBuilder(BuildFooSchema());
            var spec = builder.BuildDelete("Foo", FilterCondition.Equal(SysFields.RowId, Guid.NewGuid()));

            Assert.Contains("DELETE FROM \"TB_FOO\"", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildInsert 應使用 {0} 形式的位置佔位符（DbCommandSpec.CreateCommand 期才轉為 :p0）")]
        public void BuildInsert_UsesPositionalPlaceholders()
        {
            var dt = new DataTable();
            dt.Columns.Add("name", typeof(string));
            var row = dt.NewRow();
            row["name"] = "n";

            var builder = new OracleFormCommandBuilder(BuildFooSchema());
            var spec = builder.BuildInsert("Foo", row);

            // form-builder 層輸出位置佔位符 {0}；Oracle 的 :pN 由 CreateCommand 階段
            // 透過 DbFunc.GetDbParameterPrefix(DatabaseType.Oracle) 注入，純語法測試不涵蓋。
            Assert.Contains("{0}", spec.CommandText);
        }
    }
}
