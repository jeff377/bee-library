using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.System;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo.LoadClientSettings"/> 檔案存在路徑的覆蓋率，
    /// 以及 <c>AccessToken</c> setter 設定相同 Token 時不重設 connector 快取的行為。
    /// 因修改靜態狀態，與其他 ClientInfoState 測試同屬 collection，確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoLoadSettingsTests
    {
        private static readonly FieldInfo s_clientSettingsField =
            typeof(ClientInfo).GetField("_clientSettings", BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly FieldInfo s_systemConnectorField =
            typeof(ClientInfo).GetField("_systemConnector", BindingFlags.NonPublic | BindingFlags.Static)!;

        [Fact]
        [DisplayName("LoadClientSettings 設定檔存在時應成功反序列化並回傳非 null 的 ClientSettings")]
        public void ClientSettings_FileExists_ReturnsDeserializedSettings()
        {
            string exeName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Client";
            string fileName = $"{exeName}.Settings.xml";
            string filePath = Path.Combine(FileUtilities.GetAssemblyPath(), fileName);
            var originalCached = s_clientSettingsField.GetValue(null);

            XmlCodec.SerializeToFile(new ClientSettings(), filePath);
            try
            {
                s_clientSettingsField.SetValue(null, null);
                var result = ClientInfo.ClientSettings;
                Assert.NotNull(result);
            }
            finally
            {
                s_clientSettingsField.SetValue(null, originalCached);
                try { File.Delete(filePath); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AccessToken setter 設定相同 Token 時不應重設 _systemConnector 快取")]
        public void AccessToken_SameTokenSetTwice_ConnectorCachePreserved()
        {
            var token = Guid.NewGuid();
            var originalType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var originalConnectorCached = s_systemConnectorField.GetValue(null);

            try
            {
                ClientInfo.ApplyLoginResult(new LoginResponse
                {
                    AccessToken = token,
                    UserId = "u1",
                    UserName = "U1"
                });

                var connector = ClientInfo.SystemApiConnector;
                Assert.NotNull(connector);

                // 設定相同 Token，不應觸發 _systemConnector = null
                ClientInfo.ApplyLoginResult(new LoginResponse
                {
                    AccessToken = token,
                    UserId = "u1",
                    UserName = "U1"
                });

                var connectorAfter = s_systemConnectorField.GetValue(null);
                Assert.Same(connector, (SystemApiConnector?)connectorAfter);
            }
            finally
            {
                ClientInfo.ApplyLoginResult(new LoginResponse { AccessToken = Guid.Empty });
                ApiClientInfo.ConnectType = originalType;
                ApiClientInfo.Endpoint = originalEndpoint;
                s_systemConnectorField.SetValue(null, originalConnectorCached);
            }
        }
    }
}
