namespace Bee.Base.Data
{
    /// <summary>
    /// The single source of "now" and "today" for values that land in user data —— field defaults
    /// and the <c>Now()</c> / <c>Today()</c> expression helpers.
    /// </summary>
    /// <remarks>
    /// <b>This is not for system timestamps.</b> Audit entries, trace events and cache expiry are
    /// wall-clock-independent and use <c>DateTime.UtcNow</c> directly (ADR-032 D7/D8). This type
    /// exists only for values a user reads as a date or a time on screen.
    ///
    /// Those values must follow the <i>user's</i> time zone, not the device's and not the server
    /// machine's: a leave request defaulting to "today" means today where the user is, so someone
    /// filing a Taipei leave request from New York must not get yesterday's date (ADR-032 D12).
    /// Server and client have to agree, because calendar-day columns are never converted in transit
    /// —— a disagreement would put two different dates on one document.
    ///
    /// The user's time zone reaches here from <c>SessionInfo.TimeZone</c>, which nothing populates
    /// yet. Until it does, this clock deliberately keeps the framework's historical behaviour
    /// (machine local time). Collapsing the three former call sites into one seam is the point:
    /// wiring the real source is then a change here, not a hunt through three files.
    /// </remarks>
    public static class FrameworkClock
    {
        /// <summary>
        /// Gets the current instant as the user perceives it on a wall clock.
        /// </summary>
        /// <remarks>
        /// TODO(ADR-032 D12): derive from `SessionInfo.TimeZone` once login populates it.
        /// The open question is whether the expression helper `Now()` should keep returning the
        /// user's wall clock or switch to UTC —— see the plan's D12 for the trade-off.
        /// </remarks>
        public static DateTime Now => DateTime.Now;

        /// <summary>
        /// Gets the current calendar day as the user perceives it.
        /// </summary>
        /// <remarks>
        /// TODO(ADR-032 D12): derive from `SessionInfo.TimeZone` once login populates it.
        /// </remarks>
        public static DateTime Today => DateTime.Today;
    }
}
