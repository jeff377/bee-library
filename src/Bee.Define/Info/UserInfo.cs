namespace Bee.Define
{
    /// <summary>
    /// 前端的使用者資訊。
    /// </summary>
    public class UserInfo : IUserInfo
    {
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
    }

}
