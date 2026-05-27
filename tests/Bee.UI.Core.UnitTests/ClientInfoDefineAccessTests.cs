using System.ComponentModel;
using System.Reflection;
using Bee.Api.Core.Messages.System;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo.DefineAccess"/> getter 的覆蓋率：
    /// 驗證快取行為（兩次存取回傳同一實例）以及
    /// 當 AccessToken 變更時快取被清除（兩次存取回傳不同實例）。
    /// 因修改靜態狀態，納入 <c>ClientInfoState</c> collection 確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoDefineAccessTests
    {
        private static readonly FieldInfo s_defineAccessField =
            typeof(ClientInfo).GetField("_defineAccess", BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly FieldInfo s_systemConnectorField =
            typeof(ClientInfo).GetField("_systemConnector", BindingFlags.NonPublic | BindingFlags.Static)!;

        [Fact]
        [DisplayName("DefineAccess getter 多次存取應回傳同一快取實例")]
        public void DefineAccess_AccessedTwice_ReturnsSameInstance()
        {
            var originalDefineAccess = s_defineAccessField.GetValue(null);
            var originalSystemConnector = s_systemConnectorField.GetValue(null);
            try
            {
                s_defineAccessField.SetValue(null, null);
                s_systemConnectorField.SetValue(null, null);
                var first = ClientInfo.DefineAccess;
                var second = ClientInfo.DefineAccess;
                Assert.Same(first, second);
            }
            finally
            {
                s_defineAccessField.SetValue(null, originalDefineAccess);
                s_systemConnectorField.SetValue(null, originalSystemConnector);
            }
        }

        [Fact]
        [DisplayName("AccessToken 變更後 DefineAccess getter 應回傳新實例（快取已清除）")]
        public void DefineAccess_AfterTokenChange_ReturnsNewInstance()
        {
            var originalDefineAccess = s_defineAccessField.GetValue(null);
            var originalSystemConnector = s_systemConnectorField.GetValue(null);
            try
            {
                s_defineAccessField.SetValue(null, null);
                s_systemConnectorField.SetValue(null, null);
                var firstAccess = ClientInfo.DefineAccess;

                ClientInfo.ApplyLoginResult(new LoginResponse
                {
                    AccessToken = Guid.NewGuid(),
                    UserId = "u_test",
                    UserName = "Test"
                });

                var secondAccess = ClientInfo.DefineAccess;
                Assert.NotSame(firstAccess, secondAccess);
            }
            finally
            {
                ClientInfo.ApplyLoginResult(new LoginResponse { AccessToken = Guid.Empty });
                s_defineAccessField.SetValue(null, originalDefineAccess);
                s_systemConnectorField.SetValue(null, originalSystemConnector);
            }
        }
    }
}
