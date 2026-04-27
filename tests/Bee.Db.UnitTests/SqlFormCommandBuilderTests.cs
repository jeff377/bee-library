using System.ComponentModel;
using System.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Forms;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqlFormCommandBuilderTests
    {
        [Fact]
        [DisplayName("FormSchema 建構子 null 應擲 ArgumentNullException")]
        public void Constructor_NullFormSchema_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SqlFormCommandBuilder((FormSchema)null!));
        }

        [Fact]
        [DisplayName("ProgID 建構子找不到 FormSchema 檔案應擲例外")]
        public void Constructor_UnknownProgId_Throws()
        {
            // 實際路徑是 LocalDefineAccess → FileDefineStorage.GetFormSchema → ValidateFilePath
            // 找不到檔案會擲 FileNotFoundException，未走到原始碼裡的 ArgumentException 分支
            Assert.Throws<System.IO.FileNotFoundException>(() => new SqlFormCommandBuilder("__not_exists__"));
        }

        [Fact]
        [DisplayName("BuildInsert 應委派至 SQL Server 方言並產生 INSERT 語句")]
        public void BuildInsert_DelegatesToSqlServerDialect()
        {
            var schema = new FormSchema("X", "X");
            var table = schema.Tables!.Add("Foo", "Foo");
            table.DbTableName = "tb_foo";
            table.Fields!.AddStringField("name", "Name", 50);

            var dt = new DataTable();
            dt.Columns.Add("name", typeof(string));
            var row = dt.NewRow();
            row["name"] = "n";

            var builder = new SqlFormCommandBuilder(schema);
            var spec = builder.BuildInsert("Foo", row);

            Assert.Contains("[tb_foo]", spec.CommandText);
        }

        [Fact]
        [DisplayName("BuildUpdate 應擲 NotSupportedException")]
        public void BuildUpdate_Throws()
        {
            var builder = new SqlFormCommandBuilder(new FormSchema());

            Assert.Throws<NotSupportedException>(() => builder.BuildUpdate());
        }

        [Fact]
        [DisplayName("BuildDelete 應擲 NotSupportedException")]
        public void BuildDelete_Throws()
        {
            var builder = new SqlFormCommandBuilder(new FormSchema());

            Assert.Throws<NotSupportedException>(() => builder.BuildDelete());
        }
    }
}
