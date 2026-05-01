using System.Collections;
using System.Globalization;

namespace Bee.Base
{
    /// <summary>
    /// Framework-level value utilities. Encapsulates ERP-context defaults
    /// (<see cref="CultureInfo.InvariantCulture"/> formatting, ROC date parsing,
    /// null/DBNull-safe handling) inside these helpers so call sites do not have to
    /// pass <see cref="CultureInfo"/> or <see cref="NumberStyles"/> repeatedly. Provides
    /// emptiness checks (<c>IsEmpty</c>, <c>IsNullOrDBNull</c>) and the framework's
    /// public type-conversion API (the <c>Cxxx</c> family).
    /// </summary>
    public static class ValueUtilities
    {
        #region IsNullOrDBNull / IsEmpty

        /// <summary>
        /// Determines whether the specified value is null or DBNull.
        /// </summary>
        /// <param name="value">The value to check.</param>
        public static bool IsNullOrDBNull(object? value)
        {
            return value == null || Convert.IsDBNull(value);
        }

        /// <summary>
        /// Determines whether the specified value is empty; null and DBNull are both treated as empty.
        /// </summary>
        /// <param name="value">The value to check.</param>
        public static bool IsEmpty(object value)
        {
            switch (value)
            {
                case null:
                    return true;
                case DateTime dateTimeValue:
                    return IsEmpty(dateTimeValue);
                case string stringValue:
                    return IsEmpty(stringValue);
                case Guid guidValue:
                    return IsEmpty(guidValue);
                case IList listValue:
                    return IsEmpty(listValue);
                default:
                    return IsNullOrDBNull(value);
            }
        }

        /// <summary>
        /// Determines whether the specified string is empty.
        /// </summary>
        /// <param name="value">The string to check.</param>
        public static bool IsEmpty(string value)
        {
            return StringUtilities.IsEmpty(value, true);
        }

        /// <summary>
        /// Determines whether the specified Guid value is empty.
        /// </summary>
        /// <param name="value">The Guid value to check.</param>
        public static bool IsEmpty(Guid value)
        {
            return (value == Guid.Empty);
        }

        /// <summary>
        /// Determines whether the specified date value is empty.
        /// </summary>
        /// <param name="value">The date value to check.</param>
        public static bool IsEmpty(DateTime value)
        {
            // The minimum DateTime value in SQL databases is 1753/1/1; values earlier than this are treated as empty.
            // Null, DbNull, and DateTime.MinValue are all treated as empty.
            return (IsNullOrDBNull(value) || value < new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <summary>
        /// Determines whether the specified IList collection has no elements.
        /// </summary>
        /// <param name="value">The collection to check.</param>
        public static bool IsEmpty(IList value)
        {
            if (value != null && value.Count != 0)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Determines whether the specified IEnumerable collection has no elements.
        /// </summary>
        /// <param name="enumerable">The collection to check.</param>
        public static bool IsEmpty(IEnumerable enumerable)
        {
            if (enumerable == null) return true;

            var enumerator = enumerable.GetEnumerator();
            return !enumerator.MoveNext(); // Check whether there is at least one element
        }

        /// <summary>
        /// Determines whether the specified byte array is empty (null or zero length).
        /// </summary>
        /// <param name="data">The byte array to check.</param>
        /// <returns>True if null or empty; otherwise, false.</returns>
        public static bool IsEmpty(byte[] data)
        {
            return data == null || data.Length == 0;
        }

        #endregion

        #region CStr / CBool

        /// <summary>
        /// Converts the specified value to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value returned when conversion fails.</param>
        public static string CStr(object value, string defaultValue)
        {
            // Return the default value if null or DBNull
            if (IsNullOrDBNull(value))
                return defaultValue;
            // Return the enum name if the value is an enum type
            if (value is Enum e)
                return Enum.GetName(e.GetType(), e) ?? string.Empty;
            // Convert to string
            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Converts the specified value to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static string CStr(object value)
        {
            return CStr(value, string.Empty);
        }

        /// <summary>
        /// Converts the specified value to a boolean.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static bool CBool(string value, bool defaultValue = false)
        {
            if (StringUtilities.IsEmpty(value))
                return defaultValue;
            if (StringUtilities.IsEqualsOr(value, "1", "T", "TRUE", "Y", "YES", "是", "真"))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Converts the specified value to a boolean.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static bool CBool(object value, bool defaultValue = false)
        {
            return value is bool ? (bool)value : CBool(CStr(value), defaultValue);
        }

        #endregion

        #region CEnum

        /// <summary>
        /// Converts the specified string to an enum value (case-insensitive).
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="type">The enum type.</param>
        public static object CEnum(string value, Type type)
        {
            return Enum.Parse(type, value, true);
        }

        /// <summary>
        /// Converts the specified string to an enum value (case-insensitive).
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="value">The value to convert.</param>
        public static T CEnum<T>(string value)
        {
            return (T)CEnum(value, typeof(T));
        }

        #endregion

        #region IsNumeric / ConvertToNumber

        /// <summary>
        /// Determines whether the specified object can be treated as a numeric value (supports string, bool, and enum).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value can be converted to a number.</returns>
        public static bool IsNumeric(object value)
        {
            if (value == null)
                return false;

            // bool and enum are treated as convertible to numeric
            if (value is bool || value is Enum)
                return true;

            // Special handling for string
            var s = value as string;
            if (s != null)
                return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _);

            // Primitive numeric types
            if (value is byte || value is sbyte ||
                value is short || value is ushort ||
                value is int || value is uint ||
                value is long || value is ulong ||
                value is float || value is double || value is decimal)
                return true;

            // Last resort: convert to string and try parsing (handles reflection/dynamic objects)
            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        /// <summary>
        /// Determines whether the specified string is a numeric value of the given length.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="length">The expected length.</param>
        public static bool IsNumeric(string value, int length)
        {
            if (IsNumeric(value) && value.Length == length)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Converts the specified object to a numeric type. Supports string, bool, enum, and standard numeric types.
        /// Throws <see cref="InvalidCastException"/> if conversion is not possible.
        /// </summary>
        /// <param name="value">The value to convert; can be string, bool, enum, or a numeric type.</param>
        /// <returns>The converted number. Strings are converted to double, bools to 1 or 0, and enums to their integer value.</returns>
        /// <exception cref="InvalidCastException">Thrown when the value cannot be converted to a number.</exception>
        public static object ConvertToNumber(object value)
        {
            if (IsNullOrDBNull(value))
                return 0;

            var s = value as string;
            if (s != null)
            {
                if (IsEmpty(s))
                    return 0;

                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                    return result;
            }

            if (value is bool b)
                return b ? 1 : 0;

            if (value is Enum)
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);

            if (value is byte || value is sbyte ||
                value is short || value is ushort ||
                value is int || value is uint ||
                value is long || value is ulong ||
                value is float || value is double || value is decimal)
                return value;

            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double fallback))
                return fallback;

            throw new InvalidCastException($"Cannot convert '{value}' to number.");
        }

        #endregion

        #region CInt / CDouble / CDecimal

        /// <summary>
        /// Converts the specified value to an integer; returns the default value if conversion fails.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static int CInt(object value, int defaultValue = 0)
        {
            if (IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                if (value is Enum)
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                else
                    return Convert.ToInt32(ConvertToNumber(value), CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                return defaultValue;
            }
            catch (FormatException)
            {
                return defaultValue;
            }
            catch (OverflowException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Converts the specified value to a double; returns the default value if conversion fails.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static double CDouble(object value, double defaultValue = 0)
        {
            if (IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                return Convert.ToDouble(ConvertToNumber(value == null ? defaultValue : value), CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                return defaultValue;
            }
            catch (FormatException)
            {
                return defaultValue;
            }
            catch (OverflowException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Converts the specified value to a decimal with up to 28-29 significant digits; returns the default value if conversion fails.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static decimal CDecimal(object value, decimal defaultValue = 0)
        {
            if (IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                return Convert.ToDecimal(ConvertToNumber(value == null ? defaultValue : value), CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                return defaultValue;
            }
            catch (FormatException)
            {
                return defaultValue;
            }
            catch (OverflowException)
            {
                return defaultValue;
            }
        }

        #endregion

        #region CDateTime / CDate

        /// <summary>
        /// Converts the specified value to a DateTime. Supports Gregorian and ROC date strings
        /// (e.g. <c>20150312</c>, <c>1040312</c>); the framework parses with
        /// <see cref="CultureInfo.InvariantCulture"/> so call sites do not pass culture.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static DateTime CDateTime(object value, DateTime defaultValue = default)
        {
            if (IsNullOrDBNull(value)) { return defaultValue; }
            if (StringUtilities.IsEmpty(value)) { return defaultValue; }
            if (value is DateTime dt) { return dt; }
            if (DateTime.TryParse(CStr(value), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) { return parsed; }

            try
            {
                // Convert to string and strip date separator characters
                var sValue = CStr(value);
                return StrToDate(sValue);
            }
            catch (FormatException)
            {
                return defaultValue;
            }
            catch (OverflowException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Converts a string to a date value.
        /// </summary>
        /// <param name="value">The string describing a date.</param>
        private static DateTime StrToDate(string value)
        {
            string sValue;
            string sDate;
            int iLen;

            // Remove date separator characters
            sValue = value.Replace("/", string.Empty);
            sValue = sValue.Replace("-", string.Empty);
            // Only all-numeric strings are valid for date conversion
            if (!IsNumeric(sValue)) { return DateTime.MinValue; }
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
                return DateTime.MinValue;
        }

        /// <summary>
        /// Converts the specified value to a date (date portion only).
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static DateTime CDate(object value, DateTime defaultValue = default)
        {
            return CDateTime(value, defaultValue).Date;
        }

        #endregion

        #region CGuid

        /// <summary>
        /// Converts the specified string to a Guid value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static Guid CGuid(string value)
        {
            if (IsEmpty(value))
                return Guid.Empty;
            else
                return new Guid(value);
        }

        /// <summary>
        /// Converts the specified object to a Guid value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static Guid CGuid(object value)
        {
            if (IsNullOrDBNull(value))
                return Guid.Empty;
            else if (value is Guid g)
                return g;
            else if (value is string s)
                return CGuid(s);
            else
                return Guid.Empty;
        }

        #endregion
    }
}
