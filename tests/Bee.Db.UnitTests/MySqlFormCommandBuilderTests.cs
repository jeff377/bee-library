using System.ComponentModel;
using System.Data;
using Bee.Base.Data;
using Bee.Db.Providers.MySql;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-syntax tests for <see cref="MySqlFormCommandBuilder"/>. Verifies that the
    /// MySQL provider routes through the dialect-agnostic cores in <see cref="Bee.Db.Dml"/>
    /// with <see cref="Bee.Definition.Database.DatabaseType.MySQL"/> and emits backtick-quoted
    /// identifiers (per <see cref="DatabaseTypeExtensions.QuoteIdentifier"/>).
    /// </summary>
    [Collection("Initialize")]
    public class MySqlFormCommandBuilderTests
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
            Assert.Throws<ArgumentNullException>(() => new MySqlFormCommandBuilder((FormSchema)null!));
        }

        [Fact]
        [DisplayName("ProgID 建構子找不到 FormSchema 檔案應擲例外")]
        public void Constructor_UnknownProgId_Throws()
        {
            Assert.Throws<System.IO.FileNotFoundException>(() => new MySqlFormCommandBuilder("__not_exists__"));
        }

        [Fact]
        [DisplayName("BuildSelect 應委派至 MySQL 方言並產生 SELECT 語句（backtick 識別符）")]
        public void BuildSelect_DelegatesToMySqlDialect()
        {
            var builder = new MySqlFormCommandBuilder(BuildFooSchema());

            var spec = builder.BuildSelect("Foo", "name", null, null);

            Assert.Contains("`tb_foo`", spec.CommandText);
            Assert.Contains("`name`", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildInsert 應委派至 MySQL 方言並產生 INSERT 語句")]
        public void BuildInsert_DelegatesToMySqlDialect()
        {
            var dt = new DataTable();
            dt.Columns.Add("name", typeof(string));
            var row = dt.NewRow();
            row["name"] = "n";

            var builder = new MySqlFormCommandBuilder(BuildFooSchema());
            var spec = builder.BuildInsert("Foo", row);

            Assert.Contains("INSERT INTO `tb_foo`", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildUpdate 應委派至 MySQL 方言並產生 UPDATE 語句")]
        public void BuildUpdate_DelegatesToMySqlDialect()
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

            var builder = new MySqlFormCommandBuilder(BuildFooSchema());
            var spec = builder.BuildUpdate("Foo", row);

            Assert.Contains("UPDATE `tb_foo`", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildDelete 應委派至 MySQL 方言並產生 DELETE 語句")]
        public void BuildDelete_DelegatesToMySqlDialect()
        {
            var builder = new MySqlFormCommandBuilder(BuildFooSchema());
            var spec = builder.BuildDelete("Foo", FilterCondition.Equal(SysFields.RowId, Guid.NewGuid()));

            Assert.Contains("DELETE FROM `tb_foo`", spec.CommandText);
        }
    }
}
