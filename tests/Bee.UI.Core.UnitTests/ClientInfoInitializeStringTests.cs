using System.ComponentModel;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo.Initialize(string)"/> 單行 wrapper 的覆蓋率。
    /// 此方法內部呼叫 <see cref="ClientInfo.SetEndpoint"/>，SetEndpoint 在無本機 API 服務時
    /// 拋例外，但 <c>SetConnectType</c> 已於拋例外前完成，因此可驗證 ConnectType 副作用。
    /// 因修改靜態狀態，納入 <c>ClientInfoState</c> collection 確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoInitializeStringTests
    {
        private static string CreateTempDefinePath()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-init-str-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "SystemSettings.xml"), "<SystemSettings />");
            return tempDir;
        }

        [Fact]
        [DisplayName("Initialize(string endpoint) 有效本機路徑通過驗證後應將 ConnectType 設為 Local")]
        public void Initialize_StringEndpoint_ValidLocalPath_SetsConnectTypeToLocal()
        {
            var tempDir = CreateTempDefinePath();
            var originalConnectType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                Record.Exception(() => ClientInfo.Initialize(tempDir));
                Assert.Equal(ConnectType.Local, ApiClientInfo.ConnectType);
            }
            finally
            {
                ApiClientInfo.ConnectType = originalConnectType;
                ApiClientInfo.Endpoint = originalEndpoint;
                ApiClientInfo.SupportedConnectTypes = originalSupportedTypes;
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }
    }
}
