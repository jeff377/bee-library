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
        [DisplayName("BuildInsertCommand 應擲 NotSupportedException")]
        public void BuildInsertCommand_Throws()
        {
            var builder = new SqlFormCommandBuilder(new FormSchema());

            Assert.Throws<NotSupportedException>(() => builder.BuildInsertCommand());
        }

        [Fact]
        [DisplayName("BuildUpdateCommand 應擲 NotSupportedException")]
        public void BuildUpdateCommand_Throws()
        {
            var builder = new SqlFormCommandBuilder(new FormSchema());

            Assert.Throws<NotSupportedException>(() => builder.BuildUpdateCommand());
        }

        [Fact]
        [DisplayName("BuildDeleteCommand 應擲 NotSupportedException")]
        public void BuildDeleteCommand_Throws()
        {
            var builder = new SqlFormCommandBuilder(new FormSchema());

            Assert.Throws<NotSupportedException>(() => builder.BuildDeleteCommand());
        }
    }
}
