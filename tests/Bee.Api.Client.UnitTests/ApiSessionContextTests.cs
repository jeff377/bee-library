using System.ComponentModel;
using Bee.Api.Client.Connectors;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 驗證 per-session 狀態不再是 process-wide static。
    /// </summary>
    /// <remarks>
    /// 缺陷本身是「兩個 session 共用一份傳輸金鑰」，所以測試的重點不是「屬性存得進去」，
    /// 而是**兩個 session 彼此不可見**。
    /// </remarks>
    [Collection("ApiClientInfoState")]
    public class ApiSessionContextTests
    {
        [Fact]
        [DisplayName("兩個 session 的傳輸金鑰不應互相覆蓋")]
        public void TwoSessions_DoNotOverwriteEachOthersEncryptionKey()
        {
            var a = new ApiSessionContext { ApiEncryptionKey = [1, 2, 3] };
            var b = new ApiSessionContext { ApiEncryptionKey = [9, 9, 9] };

            Assert.Equal([1, 2, 3], a.ApiEncryptionKey);
            Assert.Equal([9, 9, 9], b.ApiEncryptionKey);
        }

        [Fact]
        [DisplayName("兩個 session 的使用者時區不應互相覆蓋")]
        public void TwoSessions_DoNotOverwriteEachOthersTimeZone()
        {
            var a = new ApiSessionContext { UserTimeZoneId = "Asia/Taipei" };
            var b = new ApiSessionContext { UserTimeZoneId = "Europe/Berlin" };

            Assert.Equal("Asia/Taipei", a.UserTimeZoneId);
            Assert.Equal("Europe/Berlin", b.UserTimeZoneId);
        }

        [Fact]
        [DisplayName("帶 session 建構的 connector 應持有該 session，而非 Ambient")]
        public void Connector_WithSession_UsesThatSession()
        {
            var session = new ApiSessionContext { UserTimeZoneId = "Asia/Taipei" };
            var connector = new SystemApiConnector(Guid.NewGuid(), session);

            Assert.Same(session, connector.Session);
            Assert.NotSame(ApiSessionContext.Ambient, connector.Session);
        }

        [Fact]
        [DisplayName("未帶 session 建構的 connector 應退回 Ambient（既有單使用者宿主行為不變）")]
        public void Connector_WithoutSession_FallsBackToAmbient()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());

            Assert.Same(ApiSessionContext.Ambient, connector.Session);
        }

        [Fact]
        [DisplayName("ApiClientInfo 的兩個 per-session 屬性應是 Ambient 的 facade")]
        public void ApiClientInfo_DelegatesToAmbient()
        {
            // 舊 API 仍可用，且與 Ambient 是同一份狀態 —— 桌面宿主不需要改任何東西。
            var originalZone = ApiClientInfo.UserTimeZoneId;
            var originalKey = ApiClientInfo.ApiEncryptionKey;
            try
            {
                ApiClientInfo.UserTimeZoneId = "Asia/Tokyo";
                Assert.Equal("Asia/Tokyo", ApiSessionContext.Ambient.UserTimeZoneId);

                ApiSessionContext.Ambient.ApiEncryptionKey = [7, 7];
                Assert.Equal([7, 7], ApiClientInfo.ApiEncryptionKey);
            }
            finally
            {
                ApiClientInfo.UserTimeZoneId = originalZone;
                ApiClientInfo.ApiEncryptionKey = originalKey;
            }
        }

        [Fact]
        [DisplayName("session 為 null 時建構 connector 應擲例外，而非靜默退回 Ambient")]
        public void Connector_NullSession_Throws()
        {
            // 靜默退回 Ambient 會讓多使用者宿主的設定錯誤變成「看起來能跑、實際共用金鑰」。
            Assert.Throws<ArgumentNullException>(
                () => new SystemApiConnector(Guid.NewGuid(), null!));
        }
    }
}
