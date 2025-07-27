using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 伺服端使用連線資訊，記錄執行階段用戶與伺服端建立連線的相關資料。
    /// </summary>
    [Serializable]
    public class SessionInfo : IKeyObject
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
        /// 用戶帳號。
        /// </summary>
        public string UserID { get; set; } = string.Empty;

        /// <summary>
        /// 用戶名稱。
        /// </summary>
        public string UserName { get; set; } = string.Empty;

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
            return $"{UserID} : {UserName}";
        }
    }
}
