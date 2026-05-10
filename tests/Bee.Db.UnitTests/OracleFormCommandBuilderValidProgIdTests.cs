using System.ComponentModel;
using Bee.Db.Providers.Oracle;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補強 <see cref="OracleFormCommandBuilder"/> progId 建構子的成功路徑覆蓋率。
    /// </summary>
    [Collection("Initialize")]
    public class OracleFormCommandBuilderValidProgIdTests
    {
        [Fact]
        [DisplayName("ProgId 建構子有效程式代碼應正常建立實例")]
        public void Constructor_ValidProgId_Succeeds()
        {
            var exception = Record.Exception(() => new OracleFormCommandBuilder("Employee"));
            Assert.Null(exception);
        }
    }
}
