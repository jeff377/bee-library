using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client;
using Bee.Api.Client.Connectors;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// 補強 <see cref="ClientInfo.ResetDefineCache"/> 的測試覆蓋率。
    /// 驗證 _defineAccess 為 null 時不拋例外（no-op），
    /// 以及 _defineAccess 為 ClientDefineAccess 時呼叫 ClearCache 不拋例外。
    /// 因修改靜態狀態，納入 ClientInfoState collection 確保串行執行。
    /// </summary>
    [Collection("ClientInfoState")]
    public class ClientInfoResetDefineCacheTests
    {
        private static readonly FieldInfo s_defineAccessField =
            typeof(ClientInfo).GetField("_defineAccess", BindingFlags.NonPublic | BindingFlags.Static)!;

        [Fact]
        [DisplayName("ResetDefineCache _defineAccess 為 null 時應直接回傳，不拋例外")]
        public void ResetDefineCache_WhenDefineAccessIsNull_DoesNotThrow()
        {
            var original = s_defineAccessField.GetValue(null);
            try
            {
                s_defineAccessField.SetValue(null, null);
                var exception = Record.Exception(() => ClientInfo.ResetDefineCache());
                Assert.Null(exception);
            }
            finally
            {
                s_defineAccessField.SetValue(null, original);
            }
        }

        [Fact]
        [DisplayName("ResetDefineCache _defineAccess 為 ClientDefineAccess 時應呼叫 ClearCache 且不拋例外")]
        public void ResetDefineCache_WhenDefineAccessIsClientDefineAccess_DoesNotThrow()
        {
            var original = s_defineAccessField.GetValue(null);
            try
            {
                var connector = new SystemApiConnector(Guid.Empty);
                var remoteAccess = new ClientDefineAccess(connector);
                s_defineAccessField.SetValue(null, remoteAccess);
                var exception = Record.Exception(() => ClientInfo.ResetDefineCache());
                Assert.Null(exception);
            }
            finally
            {
                s_defineAccessField.SetValue(null, original);
            }
        }
    }
}
