using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 後端的連線資訊，記錄執行階段用戶與伺服端建立連線的相關資料。
    /// </summary>
    [Serializable]
    public class SessionInfo : IKeyObject, IUserInfo
    {
        #region IKeyObject 介面

        /// <summary>
        /// 取得成員鍵值。
        /// </summary>
        public string GetKey()
        {
            return this.AccessToken.ToString();
        }

        #endregion

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 登入成功後 AccessToken 的有效期限（UTC 時間）。
        /// </summary>
        public DateTime ExpiredAt { get; set; }

        /// <summary>
        /// 使用者帳號。
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 使用者名稱。
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 使用者語系（如 zh-TW, en-US）
        /// </summary>
        public string Culture { get; set; } = "zh-TW";

        /// <summary>
        /// 使用者時區（建議使用 IANA，如 Asia/Taipei）
        /// </summary>
        public string TimeZone { get; set; } = "Asia/Taipei";

        /// <summary>
        /// API 傳輸加密金鑰，供用戶端與伺服端雙向資料傳輸時進行加解密使用。
        /// 此金鑰由伺服端動態產生，並於登入時傳回用戶端。
        /// </summary>
        public byte[] ApiEncryptionKey { get; set; }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{UserId} : {UserName}";
        }
    }
}
