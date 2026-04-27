using System.ComponentModel;
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
        [DisplayName("BuildInsert 應擲 NotSupportedException")]
        public void BuildInsert_Throws()
        {
            var builder = new SqlFormCommandBuilder(new FormSchema());

            Assert.Throws<NotSupportedException>(() => builder.BuildInsert());
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
