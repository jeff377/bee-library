namespace Bee.Definition.Identity
{
    /// <summary>
    /// Frontend user information.
    /// </summary>
    public class UserInfo : IUserInfo
    {
        /// <summary>
        /// Gets or sets the user account ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user culture (e.g., zh-TW, en-US).
        /// </summary>
        public string Culture { get; set; } = "zh-TW";

        /// <summary>
        /// Gets or sets the user time zone (IANA format recommended, e.g., Asia/Taipei).
        /// </summary>
        public string TimeZone { get; set; } = "Asia/Taipei";
    }

}
