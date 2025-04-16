using System;
using System.ComponentModel;

namespace Bee.Define
{
    /// <summary>
    /// 連線資訊儲存的用戶資料。
    /// </summary>
    [Serializable]
    public class TSessionUser
    {
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
        /// 效期終止時間。
        /// </summary>
        [DefaultValue(typeof(DateTime), "0001-01-01T00:00:00.0000000Z")]
        public DateTime EndTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 一次性有效。
        /// </summary>
        [DefaultValue(false)]
        public bool OneTime { get; set; } = false;

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{UserID} : {UserName}";
        }
    }
}
