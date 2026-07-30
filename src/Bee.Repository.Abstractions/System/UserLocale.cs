namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// A user's localization preferences, read from <c>st_user</c> in a single query.
    /// </summary>
    /// <param name="TimeZone">
    /// The IANA time zone id (<c>st_user.time_zone</c>), or an empty string when unset.
    /// </param>
    /// <param name="Culture">
    /// The culture name such as <c>zh-TW</c> (<c>st_user.culture</c>), or an empty string when unset.
    /// </param>
    /// <remarks>
    /// The two values travel together because login needs both and they live in the same row —
    /// reading them separately would cost an extra round trip per login for no benefit.
    /// Either may be empty; the caller decides the fallback.
    /// </remarks>
    public readonly record struct UserLocale(string TimeZone, string Culture)
    {
        /// <summary>
        /// A locale with neither value set, used when the user does not exist.
        /// </summary>
        public static UserLocale Empty { get; } = new(string.Empty, string.Empty);
    }
}
