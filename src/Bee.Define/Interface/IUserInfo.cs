using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Define
{
    /// <summary>
    /// 表示通用的使用者資訊，包含識別、語系與時區等。
    /// </summary>
    public interface IUserInfo
    {
        /// <summary>
        /// 用戶帳號。
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// 用戶名稱。
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// 使用者語系（如 zh-TW, en-US）
        /// </summary>
        string Culture { get; }

        /// <summary>
        /// 使用者時區（建議使用 IANA，如 Asia/Taipei）
        /// </summary>
        string TimeZone { get; }
    }
}
