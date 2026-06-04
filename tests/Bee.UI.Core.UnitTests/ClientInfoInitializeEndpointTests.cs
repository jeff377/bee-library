using System.ComponentModel;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo.Initialize(string)"/> 覆蓋率：
    /// 驗證以有效本機路徑呼叫時能通過驗證並設定 ConnectType。
    /// 因修改靜態狀態，納入 <c>ClientInfoState</c> collection 確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoInitializeEndpointTests
    {
        private static string CreateTempDefinePath()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-init-ep-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "SystemSettings.xml"), "<SystemSettings />");
            return tempDir;
        }

        [Fact]
        [DisplayName("Initialize(string) 有效本機路徑通過驗證後應將 ConnectType 設為 Local")]
        public void Initialize_StringEndpoint_ValidLocalPath_SetsConnectTypeToLocal()
        {
            var tempDir = CreateTempDefinePath();
            var originalConnectType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalSupportedTypes = ApiClientInfo.SupportedConnectTypes;
            try
            {
                ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Both;
                // Initialize(string) 呼叫 SetEndpoint；Validate 通過後設 ConnectType，
                // 再呼叫 SyncExecutor.Run；無本機 API 服務時 Run 拋例外向上傳播。
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
