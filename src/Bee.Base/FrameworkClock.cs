namespace Bee.Base
{
    /// <summary>
    /// Produces "today" and "now" in a given user's time zone, for values a user reads as a date or
    /// a time on screen.
    /// </summary>
    /// <remarks>
    /// <b>Not for system timestamps.</b> Audit entries, trace events and cache expiry are
    /// wall-clock-independent and use <c>DateTime.UtcNow</c> directly (ADR-032 D7/D8).
    ///
    /// The zone is passed in rather than resolved from ambient state, because there is no ambient
    /// "current user" to resolve from: <c>ISessionInfoService</c> is keyed by access token, so a
    /// server handling several users concurrently has no single session to consult. Passing the id
    /// also keeps this type in <c>Bee.Base</c>, below the identity model — callers that hold an
    /// <c>IUserInfo</c> (both <c>SessionInfo</c> and <c>UserInfo</c> implement it) simply pass its
    /// <c>TimeZone</c>.
    ///
    /// <see cref="Today"/> returns <see cref="DateOnly"/> because a calendar day is not an instant.
    /// The one place a date must still travel as <see cref="DateTime"/> is a <c>DataSet</c> cell —
    /// <c>DataColumn</c> coerces through <c>IConvertible</c>, which <see cref="DateOnly"/> does not
    /// implement, so a calendar-day column stays <c>typeof(DateTime)</c> and carries its
    /// day-versus-instant meaning in a <c>FieldDbType</c> marker instead (ADR-031). Callers writing
    /// into a <c>DataSet</c> convert at that boundary.
    /// </remarks>
    public static class FrameworkClock
    {
        /// <summary>
        /// Gets the current calendar day in the given time zone.
        /// </summary>
        /// <param name="timeZoneId">An IANA time zone id (e.g. <c>Asia/Taipei</c>); blank means UTC.</param>
        /// <exception cref="InvalidOperationException">The id is not blank and cannot be resolved.</exception>
        public static DateOnly Today(string timeZoneId) => DateOnly.FromDateTime(Now(timeZoneId));

        /// <summary>
        /// Gets the current wall-clock instant in the given time zone, with
        /// <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        /// <param name="timeZoneId">An IANA time zone id (e.g. <c>Asia/Taipei</c>); blank means UTC.</param>
        /// <exception cref="InvalidOperationException">The id is not blank and cannot be resolved.</exception>
        /// <remarks>
        /// The result is <see cref="DateTimeKind.Unspecified"/>, never <see cref="DateTimeKind.Local"/>:
        /// a wall-clock reading in a zone that is not the machine's would be mislabelled by
        /// <c>Local</c>, and <c>Local</c> shifts the reading on both wires (ADR-032 D6).
        /// </remarks>
        public static DateTime Now(string timeZoneId)
        {
            var utcNow = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(timeZoneId)) { return DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified); }

            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(utcNow, Resolve(timeZoneId)), DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Resolves a time zone id, translating a lookup failure into a diagnosable error.
        /// </summary>
        /// <param name="timeZoneId">The IANA time zone id.</param>
        /// <remarks>
        /// A failure here is never harmless, so it is not swallowed. Two causes look identical to the
        /// caller but need different fixes: a mistyped or non-IANA id is a configuration error, while
        /// a valid id that will not resolve means the runtime shipped without time zone data —
        /// the documented hazard for trimmed WASM and mobile builds. Falling back to UTC would leave
        /// every date silently wrong by the user's offset instead.
        /// </remarks>
        private static TimeZoneInfo Resolve(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException ex)
            {
                throw new InvalidOperationException(
                    $"Time zone '{timeZoneId}' was not found. Check the id is a valid IANA name, and " +
                    "that the runtime ships time zone data — a trimmed WASM or mobile build with " +
                    "InvariantGlobalization enabled has none. See docs/adr/adr-032-datetime-timezone.md.", ex);
            }
            catch (InvalidTimeZoneException ex)
            {
                throw new InvalidOperationException(
                    $"Time zone '{timeZoneId}' resolved to corrupt time zone data.", ex);
            }
        }
    }
}
