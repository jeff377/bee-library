using System;

namespace Bee.Connect
{
    /// <summary>
    /// API 用戶端應用層級的運行設定（跨 WinForms/Web/App 共用）。
    /// 僅存放「應用層級」與「連線」相關設定，不包含使用者會話狀態。
    /// </summary>
    public class ApiClientContext
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
        /// API 傳輸加密金鑰，通過 RSA 公鑰交換的金鑰。
        /// </summary>
        public static byte[] ApiEncryptionKey { get; set; } = Array.Empty<byte>();

    }
}
