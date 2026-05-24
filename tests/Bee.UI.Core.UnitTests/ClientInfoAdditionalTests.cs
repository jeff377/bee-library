using System.ComponentModel;
using Bee.Api.Core.Messages.System;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo"/> 中尚未被覆蓋的非修改路徑。
    /// 測試僅呼叫不改變可觀測公開靜態狀態的方法，無需 collection 序列化保護。
    /// </summary>
    public class ClientInfoReadOnlyTests
    {
        [Fact]
        [DisplayName("GetEndpoint 應回傳字串，不拋例外")]
        public void GetEndpoint_Default_ReturnsStringWithoutThrowing()
        {
            var result = ClientInfo.GetEndpoint();
            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("SystemApiConnector getter 應以 Lazy 模式建立非 null 實例")]
        public void SystemApiConnector_LocalConnectType_ReturnsNotNull()
        {
            var connector = ClientInfo.SystemApiConnector;
            Assert.NotNull(connector);
        }

        [Fact]
        [DisplayName("SystemApiConnector getter 多次存取應回傳同一實例（快取）")]
        public void SystemApiConnector_AccessedTwice_ReturnsSameInstance()
        {
            var first = ClientInfo.SystemApiConnector;
            var second = ClientInfo.SystemApiConnector;
            Assert.Same(first, second);
        }

        [Fact]
        [DisplayName("DefineAccess getter 應以 Lazy 模式建立非 null 的 IDefineAccess 實例")]
        public void DefineAccess_LocalConnectType_ReturnsNotNull()
        {
            var access = ClientInfo.DefineAccess;
            Assert.NotNull(access);
        }

        [Fact]
        [DisplayName("CreateFormApiConnector 應回傳非 null 的 FormApiConnector 實例")]
        public void CreateFormApiConnector_LocalConnectType_ReturnsNotNull()
        {
            var connector = ClientInfo.CreateFormApiConnector("TestProg");
            Assert.NotNull(connector);
        }
    }

    /// <summary>
    /// 補強 <see cref="ClientInfo"/> 中會修改靜態狀態的路徑。
    /// 與 <c>EndpointStorageTests</c> 同屬 <c>ClientInfoState</c> collection，確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoMutatingTests
    {
        [Fact]
        [DisplayName("ApplyLoginResult 傳入有效 LoginResponse 應設定 AccessToken 與 UserInfo")]
        public void ApplyLoginResult_ValidLoginResponse_SetsAccessTokenAndUserInfo()
        {
            var token = Guid.NewGuid();
            var response = new LoginResponse
            {
                AccessToken = token,
                UserId = "u001",
                UserName = "測試使用者"
            };
            try
            {
                ClientInfo.ApplyLoginResult(response);
                Assert.Equal(token, ClientInfo.AccessToken);
                Assert.NotNull(ClientInfo.UserInfo);
                Assert.Equal("u001", ClientInfo.UserInfo!.UserId);
                Assert.Equal("測試使用者", ClientInfo.UserInfo.UserName);
            }
            finally
            {
                ClientInfo.ApplyLoginResult(new LoginResponse { AccessToken = Guid.Empty });
            }
        }

        [Fact]
        [DisplayName("ApplyLoginResult 以不同 Token 連續呼叫兩次，應以最後一次結果為準")]
        public void ApplyLoginResult_CalledTwice_LastResultWins()
        {
            var tokenA = Guid.NewGuid();
            var tokenB = Guid.NewGuid();
            try
            {
                ClientInfo.ApplyLoginResult(new LoginResponse
                {
                    AccessToken = tokenA,
                    UserId = "userA",
                    UserName = "A"
                });
                ClientInfo.ApplyLoginResult(new LoginResponse
                {
                    AccessToken = tokenB,
                    UserId = "userB",
                    UserName = "B"
                });
                Assert.Equal(tokenB, ClientInfo.AccessToken);
                Assert.Equal("userB", ClientInfo.UserInfo!.UserId);
            }
            finally
            {
                ClientInfo.ApplyLoginResult(new LoginResponse { AccessToken = Guid.Empty });
            }
        }
    }
}
