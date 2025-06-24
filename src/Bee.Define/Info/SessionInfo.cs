using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 連線資訊，記錄執行階段用戶與伺服端建立連線的相關資訊。
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
        /// 用戶帳號。
        /// </summary>
        public string UserID { get; set; } = string.Empty;

        /// <summary>
        /// 用戶名稱。
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{UserID} : {UserName}";
        }
    }
}
