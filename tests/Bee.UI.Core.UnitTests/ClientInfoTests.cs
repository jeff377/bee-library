using System.ComponentModel;

namespace Bee.UI.Core.UnitTests
{
    /// <summary>
    /// <see cref="ClientInfo"/> 預設狀態的 smoke 測試。
    /// <para>
    /// 多數測試僅讀取 default getter,但 <c>ClientSettings_NoFile_ReturnsNonNull</c> 觸發
    /// <see cref="ClientInfo.ClientSettings"/> 的 lazy getter,在無檔案時會建立並**快取**一個
    /// 空白 <c>ClientSettings</c> 至 static 欄位——這是對 process-wide static state 的寫入。
    /// 因此本類別與其他會碰 <see cref="ClientInfo.ClientSettings"/> 的測試同列
    /// <c>[Collection("ClientInfoState")]</c> 序列化,避免 2-core CI 下與
    /// <c>EndpointStorageTests</c> 並行時 race(空白 instance 蓋掉剛寫入的 Endpoint)。
    /// </para>
    /// </summary>
    [Collection("ClientInfoState")]
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

        [Fact]
        [DisplayName("ClientInfo.UserInfo 未登入時預設為 null")]
        public void UserInfo_Default_IsNull()
        {
            Assert.Null(ClientInfo.UserInfo);
        }

        [Fact]
        [DisplayName("ClientInfo.AllowGenerateSettings 預設為 false")]
        public void AllowGenerateSettings_Default_IsFalse()
        {
            Assert.False(ClientInfo.AllowGenerateSettings);
        }

        [Fact]
        [DisplayName("ClientInfo.UIViewService 預設為 null")]
        public void UIViewService_Default_IsNull()
        {
            Assert.Null(ClientInfo.UIViewService);
        }

        [Fact]
        [DisplayName("ClientInfo.Arguments 預設為 null")]
        public void Arguments_Default_IsNull()
        {
            Assert.Null(ClientInfo.Arguments);
        }

        [Fact]
        [DisplayName("ClientInfo.ApplyLoginResult 傳入 null 應拋 ArgumentNullException")]
        public void ApplyLoginResult_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ClientInfo.ApplyLoginResult(null!));
        }
    }
}
