using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Definition
{
    /// <summary>
    /// Represents general user information, including identity, culture, and time zone.
    /// </summary>
    public interface IUserInfo
    {
        /// <summary>
        /// Gets the user account ID.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Gets the user name.
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Gets the user culture (e.g., zh-TW, en-US).
        /// </summary>
        string Culture { get; }

        /// <summary>
        /// Gets the user time zone (IANA format recommended, e.g., Asia/Taipei).
        /// </summary>
        string TimeZone { get; }
    }
}
