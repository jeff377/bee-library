using System.Globalization;

namespace Bee.Base
{
    /// <summary>
    /// Date and time conversions for <see cref="ValueUtilities"/>.
    /// </summary>
    /// <remarks>
    /// Split from the main file once it passed 600 lines. These conversions carry their own
    /// domain concerns — the SQL minimum date, culture-dependent <c>DateOnly</c> parsing, the
    /// invariant round-trip formats — none of which the numeric or string conversions share.
    /// </remarks>
    public static partial class ValueUtilities
    {
        #region CDateTime / CDateOnly

        /// <summary>
        /// Converts the specified value to an instant, or <see langword="null"/> when the value is
        /// unset or is not a recognisable date. Supports Gregorian and ROC date strings
        /// (e.g. <c>20150312</c>, <c>1040312</c>); the framework parses with
        /// <see cref="CultureInfo.InvariantCulture"/> so call sites do not pass culture.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <remarks>
        /// The whole temporal family — <see cref="CDateTime(object?)"/>,
        /// <see cref="CDateOnly(object?)"/>, <see cref="CTimeOnly(object?)"/> — returns a nullable
        /// from its one-argument form, so "unset" is a case the compiler makes the caller handle
        /// rather than a sentinel value they have to remember to compare against. Pass a default
        /// explicitly (<see cref="CDateTime(object?, DateTime)"/>) when a non-null value is wanted;
        /// stating the fallback at the call site is what makes it visible.
        /// </remarks>
        public static DateTime? CDateTime(object? value)
        {
            if (value is null || value is DBNull) { return null; }
            if (StringUtilities.IsEmptyText(value)) { return null; }
            if (value is DateTime dt) { return dt; }
            // Handled before the string path: `DateOnly.ToString()` is culture-dependent, so parsing it
            // back with `InvariantCulture` succeeds only where the two formats happen to agree.
            if (value is DateOnly date) { return date.ToDateTime(TimeOnly.MinValue); }
            if (DateTime.TryParse(CStr(value), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) { return parsed; }

            try
            {
                // Convert to string and strip date separator characters
                var sValue = CStr(value);
                return StrToDate(sValue);
            }
            catch (FormatException)
            {
                return null;
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts the specified value to an instant, falling back to <paramref name="defaultValue"/>
        /// when the value is unset or is not a recognisable date.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The value to use when conversion yields nothing.</param>
        public static DateTime CDateTime(object? value, DateTime defaultValue)
            => CDateTime(value) ?? defaultValue;

        /// <summary>
        /// Converts a string to a date value, or <see langword="null"/> when the string is not a
        /// recognisable date form.
        /// </summary>
        /// <param name="value">The string describing a date.</param>
        private static DateTime? StrToDate(string value)
        {
            string sValue;
            string sDate;
            int iLen;

            // Remove date separator characters
            sValue = value.Replace("/", string.Empty);
            sValue = sValue.Replace("-", string.Empty);
            // Only all-numeric strings are valid for date conversion
            if (!IsNumeric(sValue)) { return null; }
            // Attempt date conversion based on the string length
            iLen = sValue.Length;
            switch (iLen)
            {
                case 8: // 8-digit Gregorian date, e.g. 20150312
                    sDate = sValue.Insert(4, "-").Insert(7, "-");
                    break;
                case 7: // 7-digit ROC date, e.g. 1040312
                    sDate = StringUtilities.Format("{0}-{1}-{2}", CInt(sValue.Substring(0, 3)) + 1911,
                        sValue.Substring(3, 2), sValue.Substring(5, 2));
                    break;
                case 6: // 6-digit Gregorian year-month, e.g. 201503
                    sDate = CStr(value).Insert(4, "-") + "-01";
                    break;
                case 5: // 5-digit ROC year-month, e.g. 10403
                    sDate = StringUtilities.Format("{0}-{1}-01", CInt(sValue.Substring(0, 3)) + 1911,
                        sValue.Substring(3, 2));
                    break;
                case 4: // 4-digit Gregorian year, e.g. 2015
                    sDate = StringUtilities.Format("{0}-01-01", sValue);
                    break;
                case 3: // 3-digit ROC year, e.g. 104
                    sDate = StringUtilities.Format("{0}-01-01", CInt(sValue) + 1911);
                    break;
                default:
                    sDate = string.Empty;
                    break;
            }

            if (StringUtilities.IsNotEmpty(sDate))
                return Convert.ToDateTime(sDate, CultureInfo.InvariantCulture);
            else
                return null;
        }

        /// <summary>
        /// Converts the specified value to a calendar day, discarding any time of day.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <remarks>
        /// Returns <see cref="DateOnly"/> so a calendar day is distinguishable from an instant at the
        /// point of use, the same distinction the definition layer draws between
        /// <see cref="Bee.Base.Data.FieldDbType.Date"/> and <see cref="Bee.Base.Data.FieldDbType.DateTime"/>. Use
        /// <see cref="CDateTime(object, DateTime)"/> when a <see cref="DateTime"/> is wanted — notably
        /// when writing back into a <see cref="System.Data.DataColumn"/>, which stores calendar-day
        /// columns as <see cref="DateTime"/> and rejects a <see cref="DateOnly"/> value.
        /// </remarks>
        public static DateOnly? CDateOnly(object? value)
        {
            return CDateTime(value) is { } instant ? DateOnly.FromDateTime(instant) : null;
        }

        /// <summary>
        /// Converts the specified value to a calendar day, falling back to
        /// <paramref name="defaultValue"/> when the value is unset or is not a recognisable date.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The value to use when conversion yields nothing.</param>
        public static DateOnly CDateOnly(object? value, DateOnly defaultValue)
            => CDateOnly(value) ?? defaultValue;

        #endregion

        #region CTimeOnly

        /// <summary>
        /// The storage and display format of a time of day: fixed-width 24-hour <c>"HH:mm"</c>.
        /// </summary>
        /// <remarks>
        /// Fixed width is what makes lexicographic ordering match chronological ordering, so a
        /// <c>char(5)</c> column sorts and range-scans correctly without a cast. Display uses the
        /// same format as storage, so no culture-aware formatting is involved anywhere.
        /// </remarks>
        public const string TimeOnlyFormat = "HH:mm";

        /// <summary>
        /// The storage width of a time of day, in characters — the width of a <c>char(5)</c> column.
        /// </summary>
        /// <remarks>
        /// Derived from <see cref="TimeOnlyFormat"/> so the two cannot drift apart.
        /// </remarks>
        public static readonly int TimeOnlyLength = TimeOnlyFormat.Length;

        /// <summary>
        /// Converts the specified value to a time of day, or <see langword="null"/> when the value is
        /// unset or is not a well-formed time.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <remarks>
        /// Returns a nullable <see cref="TimeOnly"/> rather than following the default-value shape of
        /// the rest of the <c>Cxxx</c> family, because a time of day has no spare value to stand for
        /// "unset": <c>default(TimeOnly)</c> is <c>00:00</c>, a perfectly legal midnight. Handing back
        /// a default would silently turn an unfilled field into midnight. An unset time is the empty
        /// string in storage and <see langword="null"/> here.
        /// <para>
        /// Parsing is lenient about width (<c>"8:30"</c> is accepted) and accepts a
        /// <see cref="DateTime"/> or <see cref="TimeSpan"/> source, but never accepts an out-of-range
        /// value: this method is the framework's gate against the wider range a <c>char(5)</c> column
        /// or a <see cref="TimeSpan"/> can physically hold (ADR-033).
        /// </para>
        /// </remarks>
        public static TimeOnly? CTimeOnly(object? value)
        {
            switch (value)
            {
                case null:
                case DBNull:
                    return null;
                case TimeOnly timeOnly:
                    return timeOnly;
                case DateTime dateTime:
                    return TimeOnly.FromDateTime(dateTime);
                case TimeSpan timeSpan:
                    return timeSpan >= TimeSpan.Zero && timeSpan < TimeSpan.FromDays(1)
                        ? TimeOnly.FromTimeSpan(timeSpan)
                        : null;
            }

            var text = CStr(value).Trim();
            if (IsEmpty(text)) { return null; }
            return TimeOnly.TryParseExact(text, ["HH:mm", "H:mm"], CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result) ? result : null;
        }

        /// <summary>
        /// Converts the specified value to a time of day, falling back to
        /// <paramref name="defaultValue"/> when the value is unset or malformed.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The value to use when conversion yields nothing.</param>
        /// <remarks>
        /// Stating the fallback here is deliberate: <c>default(TimeOnly)</c> is midnight, a legal
        /// time of day, so a silently-defaulted unset field would be indistinguishable from one
        /// genuinely set to <c>00:00</c>.
        /// </remarks>
        public static TimeOnly CTimeOnly(object? value, TimeOnly defaultValue)
            => CTimeOnly(value) ?? defaultValue;

        /// <summary>
        /// Converts the specified value to the storage form of a time of day: a fixed-width
        /// <c>"HH:mm"</c> string, or the empty string when the value is unset or malformed.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <remarks>
        /// This is the normalising counterpart of <see cref="CTimeOnly(object?)"/>: it is what turns
        /// <c>"8:30"</c> into <c>"08:30"</c> so the fixed-width ordering guarantee holds for every
        /// value that reaches the database.
        /// </remarks>
        public static string CTimeString(object? value)
        {
            var time = CTimeOnly(value);
            return time?.ToString(TimeOnlyFormat, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        #endregion
    }
}
