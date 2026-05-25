using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo.ParseCommandLineArgs"/> 私有方法的測試覆蓋率。
    /// 此類別為純讀取操作，無需加入 ClientInfoState collection。
    /// </summary>
    public class ClientInfoParseArgsTests
    {
        [Fact]
        [DisplayName("ParseCommandLineArgs 透過反射呼叫應回傳非 null 的字典")]
        public void ParseCommandLineArgs_InvokedViaReflection_ReturnsNonNull()
        {
            var method = typeof(ClientInfo).GetMethod(
                "ParseCommandLineArgs",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var result = method!.Invoke(null, null);
            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs 回傳的字典應支援大小寫不分的鍵查詢（OrdinalIgnoreCase）")]
        public void ParseCommandLineArgs_Result_SupportsCaseInsensitiveKeys()
        {
            var method = typeof(ClientInfo).GetMethod(
                "ParseCommandLineArgs",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var result = (Dictionary<string, string>)method!.Invoke(null, null)!;
            result["TestKey"] = "value";
            Assert.True(result.ContainsKey("testkey"));
            Assert.True(result.ContainsKey("TESTKEY"));
        }
    }

    /// <summary>
    /// 補強 <see cref="ClientInfo"/> <c>SetConnectType</c> 私有方法與遠端連線快取路徑的測試覆蓋率。
    /// 因修改靜態狀態，與 EndpointStorageTests 同屬 ClientInfoState collection，確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoConnectorTests
    {
        private static readonly Type[] s_setConnectTypeParams = [typeof(ConnectType), typeof(string)];
        private static readonly object[] s_remoteConnectTypeArgs = [ConnectType.Remote, "http://remote.example.com"];
        private static readonly object[] s_localConnectTypeArgs = [ConnectType.Local, string.Empty];

        private static MethodInfo GetSetConnectTypeMethod()
        {
            var method = typeof(ClientInfo).GetMethod(
                "SetConnectType",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_setConnectTypeParams,
                null);
            Assert.NotNull(method);
            return method!;
        }

        [Fact]
        [DisplayName("SetConnectType Local 應將 ApiClientInfo.ConnectType 設為 Local")]
        public void SetConnectType_LocalEndpoint_SetsConnectTypeToLocal()
        {
            var method = GetSetConnectTypeMethod();
            var originalType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            try
            {
                method.Invoke(null, s_localConnectTypeArgs);
                Assert.Equal(ConnectType.Local, ApiClientInfo.ConnectType);
                Assert.Equal(string.Empty, ApiClientInfo.Endpoint);
            }
            finally
            {
                ApiClientInfo.ConnectType = originalType;
                ApiClientInfo.Endpoint = originalEndpoint;
            }
        }

        [Fact]
        [DisplayName("SetConnectType Remote 應將 ApiClientInfo.ConnectType 設為 Remote 並更新 Endpoint")]
        public void SetConnectType_RemoteEndpoint_SetsConnectTypeAndEndpoint()
        {
            var method = GetSetConnectTypeMethod();
            var originalType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            try
            {
                method.Invoke(null, s_remoteConnectTypeArgs);
                Assert.Equal(ConnectType.Remote, ApiClientInfo.ConnectType);
                Assert.Equal("http://remote.example.com", ApiClientInfo.Endpoint);
            }
            finally
            {
                ApiClientInfo.ConnectType = originalType;
                ApiClientInfo.Endpoint = originalEndpoint;
            }
        }

        [Fact]
        [DisplayName("CreateFormApiConnector Remote 連線類型應回傳非 null 的 FormApiConnector")]
        public void CreateFormApiConnector_RemoteConnectType_ReturnsNonNullConnector()
        {
            var originalType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            try
            {
                ApiClientInfo.ConnectType = ConnectType.Remote;
                ApiClientInfo.Endpoint = "http://remote.example.com";
                var connector = ClientInfo.CreateFormApiConnector("TestProg");
                Assert.NotNull(connector);
            }
            finally
            {
                ApiClientInfo.ConnectType = originalType;
                ApiClientInfo.Endpoint = originalEndpoint;
            }
        }

        [Fact]
        [DisplayName("SystemApiConnector getter Remote 連線類型應建立並回傳非 null 的遠端 Connector")]
        public void SystemApiConnector_RemoteConnectType_ReturnsNonNullConnector()
        {
            var originalType = ApiClientInfo.ConnectType;
            var originalEndpoint = ApiClientInfo.Endpoint;
            var sysConnField = typeof(ClientInfo).GetField(
                "_systemConnector", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(sysConnField);
            var originalConnector = sysConnField!.GetValue(null);
            try
            {
                ApiClientInfo.ConnectType = ConnectType.Remote;
                ApiClientInfo.Endpoint = "http://remote.example.com";
                sysConnField.SetValue(null, null);
                var connector = ClientInfo.SystemApiConnector;
                Assert.NotNull(connector);
            }
            finally
            {
                ApiClientInfo.ConnectType = originalType;
                ApiClientInfo.Endpoint = originalEndpoint;
                sysConnField.SetValue(null, originalConnector);
            }
        }
    }
}
