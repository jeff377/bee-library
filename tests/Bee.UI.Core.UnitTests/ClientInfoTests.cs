using System.ComponentModel;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// <see cref="ClientInfo"/> 預設狀態的 smoke 測試。
    /// <para>
    /// 本測試僅讀取 <see cref="ClientInfo"/> 的 default getter,**不修改** static state,
    /// 避免與其他測試 process-wide race。深度行為測試(Initialize / SetEndpoint 等
    /// 會 mutate static state 的路徑)留待後續獨立任務,需以 collection 序列化保護。
    /// </para>
    /// </summary>
    public class ClientInfoTests
    {
        [Fact]
        [DisplayName("ClientInfo.EndpointStorage 預設應為 EndpointStorage 實例")]
        public void EndpointStorage_Default_IsEndpointStorageInstance()
        {
            Assert.NotNull(ClientInfo.EndpointStorage);
            Assert.IsType<EndpointStorage>(ClientInfo.EndpointStorage);
        }

        [Fact]
        [DisplayName("ClientInfo.AccessToken 未登入時預設為 Guid.Empty")]
        public void AccessToken_Default_IsEmpty()
        {
            Assert.Equal(Guid.Empty, ClientInfo.AccessToken);
        }

        [Fact]
        [DisplayName("ClientInfo.ClientSettings 無檔案時應回傳空白 ClientSettings,不拋例外")]
        public void ClientSettings_NoFile_ReturnsNonNull()
        {
            // 測試 process 下 {ExeName}.Settings.xml 不存在;
            // ClientSettings getter 應 fallback 建立新的空白 ClientSettings。
            var settings = ClientInfo.ClientSettings;
            Assert.NotNull(settings);
        }
    }
}
