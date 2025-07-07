using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 前端資訊，記錄用戶端在運行期間的參數及環境設置。
    /// </summary>
    public class FrontendInfo
    {
        /// <summary>
        /// 程式支援的服務連線方式。
        /// </summary>
        public static SupportedConnectTypes SupportedConnectTypes { get; set; } = SupportedConnectTypes.Both;

        /// <summary>
        /// 服務連線方式。
        /// </summary>
        public static ConnectType ConnectType { get; set; } = ConnectType.Local;

        /// <summary>
        /// API 服務端點，一般由設定檔取得服務端點。
        /// </summary>
        public static string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// API KEY，一般由設定檔取得 API KEY。
        /// </summary>
        public static string ApiKey { get; set; } = "46CA0967-EC64-4F96-B502-139BE8FF8DAC";

        /// <summary>
        /// 存取令牌，登入後取得存取令牌。
        /// </summary>
        public static Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// API 傳輸加密金錀，通過 RSA 公鑰交換的金鑰。
        /// </summary>
        public static byte[] ApiEncryptionKey { get; set; } = Array.Empty<byte>();

    }
}
