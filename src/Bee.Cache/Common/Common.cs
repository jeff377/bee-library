using System;

namespace Bee.Cache
{
    #region 列舉型別

    /// <summary>
    /// 快取條件相依的時間類型。
    /// </summary>
    public enum CacheTimeKind
    {
        /// <summary>
        /// 相對時間。
        /// </summary>
        SlidingTime,
        /// <summary>
        /// 絕對時間。
        /// </summary>
        AbsoluteTime
    }

    #endregion
}
