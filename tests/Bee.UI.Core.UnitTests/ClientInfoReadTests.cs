using System.ComponentModel;
using System.Reflection;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補充 <see cref="ClientInfo"/> 的讀取路徑測試，涵蓋 <c>GetEndpoint</c> 與
    /// <c>ParseCommandLineArgs</c> 等不修改 static state 的公開 / 私有方法。
    /// </summary>
    public class ClientInfoReadTests
    {
        [Fact]
        [DisplayName("ClientInfo.GetEndpoint 預設狀態應回傳空字串")]
        public void GetEndpoint_DefaultState_ReturnsEmptyString()
        {
            var result = ClientInfo.GetEndpoint();
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs 應回傳非 null 的 Dictionary<string, string>")]
        public void ParseCommandLineArgs_ReturnsNonNullDictionary()
        {
            var method = typeof(ClientInfo).GetMethod(
                "ParseCommandLineArgs",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var result = method!.Invoke(null, null);
            Assert.IsAssignableFrom<Dictionary<string, string>>(result);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs 回傳的字典不含 null 值")]
        public void ParseCommandLineArgs_ReturnsDictionaryWithNoNullValues()
        {
            var method = typeof(ClientInfo).GetMethod(
                "ParseCommandLineArgs",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var result = (Dictionary<string, string>)method!.Invoke(null, null)!;
            Assert.All(result, kvp =>
            {
                Assert.NotNull(kvp.Key);
                Assert.NotNull(kvp.Value);
            });
        }
    }
}
