using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using Bee.Core.Data;
using Bee.Core.Serialization;

namespace Bee.Core
{
    /// <summary>
    /// Core utility library.
    /// </summary>
    public static class BaseFunc
    {
        /// <summary>
        /// Determines whether the specified value is DBNull.
        /// </summary>
        /// <param name="value">The value to check.</param>
        public static bool IsDBNull(object value)
        {
            return Convert.IsDBNull(value);
        }

        /// <summary>
        /// Determines whether the specified value is null or DBNull.
        /// </summary>
        /// <param name="value">The value to check.</param>
        public static bool IsNullOrDBNull(object value)
        {
            return value == null || Convert.IsDBNull(value);
        }

        /// <summary>
        /// Determines whether the specified byte array is null or has zero length.
        /// </summary>
        /// <param name="bytes">The byte array to check.</param>
        /// <returns>True if null or empty; otherwise, false.</returns>
        public static bool IsNullOrEmpty(byte[] bytes)
        {
            return bytes == null || bytes.Length == 0;
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
            return StrFunc.IsEmpty(value, true);
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
            // The minimum DateTime value in SQL databases is 1753/1/1; values earlier than this are treated as empty
            // Null, DbNull, and DateTime.MinValue are all treated as empty
            return (IsNullOrDBNull(value) || value < new DateTime(1753, 1, 1));
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

        /// <summary>
        /// Gets the name of the specified enum member.
        /// </summary>
        /// <param name="value">The enum value.</param>
        public static string GetEnumName(Enum value)
        {
            return Enum.GetName(value.GetType(), value);
        }

        /// <summary>
        /// Converts the specified value to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value returned when conversion fails.</param>
        public static string CStr(object value, string defaultValue)
        {
            // Return the default value if null or DBNull
            if (BaseFunc.IsNullOrDBNull(value))
                return defaultValue;
            // Return the enum name if the value is an enum type
            if (value is Enum e)
                return GetEnumName(e);
            // Convert to string
            return value.ToString();
        }

        /// <summary>
        /// Converts the specified value to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static string CStr(object value)
        {
            return BaseFunc.CStr(value, string.Empty);
        }

        /// <summary>
        /// Converts the specified value to a boolean.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static bool CBool(string value, bool defaultValue = false)
        {
            if (StrFunc.IsEmpty(value))
                return defaultValue;
            if (StrFunc.IsEqualsOr(value, "1", "T", "TRUE", "Y", "YES", "是", "真"))
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

        /// <summary>
        /// Converts the specified string to an enum value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="type">The enum type.</param>
        public static object CEnum(string value, Type type)
        {
            return Enum.Parse(type, value, true);
        }

        /// <summary>
        /// Converts the specified string to an enum value.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="value">The value to convert.</param>
        public static T CEnum<T>(string value)
        {
            return (T)CEnum(value, typeof(T));
        }

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
            if (BaseFunc.IsNumeric(value) && StrFunc.Length(value) == length)
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

                double result;
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    return result;
            }

            if (value is bool)
                return (bool)value ? 1 : 0;

            if (value is Enum)
                return Convert.ToInt32(value);

            if (value is byte || value is sbyte ||
                value is short || value is ushort ||
                value is int || value is uint ||
                value is long || value is ulong ||
                value is float || value is double || value is decimal)
                return value;

            double fallback;
            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out fallback))
                return fallback;

            throw new InvalidCastException($"Cannot convert '{value}' to number.");
        }

        /// <summary>
        /// Converts the specified value to an integer; returns the default value if conversion fails.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static int CInt(object value, int defaultValue = 0)
        {
            if (BaseFunc.IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                if (value is Enum)
                    return Convert.ToInt32(value);
                else
                    return Convert.ToInt32(ConvertToNumber(value));
            }
            catch
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
            if (BaseFunc.IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                return Convert.ToDouble(ConvertToNumber(value == null ? defaultValue : value));
            }
            catch
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
            if (BaseFunc.IsNullOrDBNull(value)) { return defaultValue; }

            try
            {
                return Convert.ToDecimal(ConvertToNumber(value == null ? defaultValue : value));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Converts the specified value to a DateTime.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The default value.</param>
        public static DateTime CDateTime(object value, DateTime defaultValue = default)
        {
            string sValue;

            if (IsNullOrDBNull(value)) { return defaultValue; }
            if (StrFunc.IsEmpty(value)) { return defaultValue; }
            if (DateTimeFunc.IsDate(value)) { return Convert.ToDateTime(value); }

            try
            {
                // Convert to string and strip date separator characters
                sValue = BaseFunc.CStr(value);
                return StrToDate(sValue);
            }
            catch
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
            if (!BaseFunc.IsNumeric(sValue)) { return DateTime.MinValue; }
            // Attempt date conversion based on the string length
            iLen = StrFunc.Length(sValue);
            switch (iLen)
            {
                case 8: // 8-digit Gregorian date, e.g. 20150312
                    sDate = sValue.Insert(4, "-").Insert(7, "-");
                    break;
                case 7: // 7-digit ROC date, e.g. 1040312
                    sDate = StrFunc.Format("{0}-{1}-{2}", BaseFunc.CInt(StrFunc.Left(sValue, 3)) + 1911,
                        StrFunc.Substring(sValue, 3, 2), StrFunc.Substring(sValue, 5, 2));
                    break;
                case 6: // 6-digit Gregorian year-month, e.g. 201503
                    sDate = BaseFunc.CStr(value).Insert(4, "-") + "-01";
                    break;
                case 5: // 5-digit ROC year-month, e.g. 10403
                    sDate = StrFunc.Format("{0}-{1}-01", BaseFunc.CInt(StrFunc.Left(sValue, 3)) + 1911,
                        StrFunc.Substring(sValue, 3, 2));
                    break;
                case 4: // 4-digit Gregorian year, e.g. 2015
                    sDate = StrFunc.Format("{0}-01-01", sValue);
                    break;
                case 3: // 3-digit ROC year, e.g. 104
                    sDate = StrFunc.Format("{0}-01-01", BaseFunc.CInt(sValue) + 1911);
                    break;
                default:
                    sDate = string.Empty;
                    break;
            }

            if (StrFunc.IsNotEmpty(sDate))
                return Convert.ToDateTime(sDate);
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
            else if (value is Guid)
                return (Guid)value;
            else if (value is string)
                return CGuid((string)value);
            else
                return Guid.Empty;
        }

        /// <summary>
        /// Converts the specified value to the appropriate field data type.
        /// </summary>
        /// <param name="dbType">The field data type.</param>
        /// <param name="value">The input value.</param>
        public static object CFieldValue(FieldDbType dbType, object value)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return CStr(value);
                case FieldDbType.Boolean:
                    return CBool(value);
                case FieldDbType.Integer:
                    return CInt(value);
                case FieldDbType.Decimal:
                case FieldDbType.Currency:
                    return CDecimal(value);
                case FieldDbType.Date:
                    return CDate(value);
                case FieldDbType.DateTime:
                    return CDateTime(value);
                case FieldDbType.Guid:
                    return CGuid(value);
                default:
                    return value;
            }
        }

        /// <summary>
        /// Converts the specified value to a database-ready field value.
        /// </summary>
        /// <param name="dbType">The field data type.</param>
        /// <param name="value">The input value.</param>
        public static object CDbFieldValue(FieldDbType dbType, object value)
        {
            if (value is DateTime)
            {
                if (BaseFunc.CDateTime(value) == DateTime.MinValue)
                    return DBNull.Value;
            }
            return CFieldValue(dbType, value);
        }

        /// <summary>
        /// Sets the serialization state on the specified object.
        /// </summary>
        /// <param name="objectSerialize">The object serialization interface.</param>
        /// <param name="serializeState">The serialization state to set.</param>
        public static void SetSerializeState(IObjectSerialize objectSerialize, SerializeState serializeState)
        {
            objectSerialize?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Determines whether the value is empty during serialization; null and DBNull are both treated as empty.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        /// <param name="value">The value to check.</param>
        public static bool IsSerializeEmpty(SerializeState serializeState, object value)
        {
            if (serializeState != SerializeState.Serialize) { return false; }

            switch (value)
            {
                case null:
                    return true;
                case IObjectSerializeEmpty objectSerializeEmpty:
                    return objectSerializeEmpty.IsSerializeEmpty;
                case IList listValue:
                    return IsEmpty(listValue);
                case IEnumerable enumerableValue:
                    return IsEmpty(enumerableValue);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Generates a new Guid value.
        /// </summary>
        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// Generates a new Guid value as a string.
        /// </summary>
        public static string NewGuidString()
        {
            return NewGuid().ToString();
        }

        /// <summary>
        /// Returns a cryptographically secure random integer.
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The exclusive maximum value.</param>
        /// <returns>A random integer between min (inclusive) and max (exclusive).</returns>
        public static int RndInt(int min, int max)
        {
#if NET8_0_OR_GREATER
            return RandomNumberGenerator.GetInt32(min, max);
#else
            if (min >= max)
                throw new ArgumentOutOfRangeException(nameof(min), "min must be less than max.");

            long diff = (long)max - min;
            byte[] uint32Buffer = new byte[4];

            using (var rng = RandomNumberGenerator.Create())
            {
                while (true)
                {
                    rng.GetBytes(uint32Buffer);
                    uint rand = BitConverter.ToUInt32(uint32Buffer, 0);

                    long remainder = rand % diff;
                    if (rand - remainder + (diff - 1) >= rand)
                        return (int)(min + remainder);
                }
            }
#endif
        }


        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="typeName">The type name.</param>
        /// <param name="args">Constructor arguments.</param>
        public static object CreateInstance(string assemblyName, string typeName, params object[] args)
        {
            return AssemblyLoader.CreateInstance(assemblyName, typeName, args);
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="typeName">The type name, in the format "Bee.Business.TBusinessObject, Bee.Business" or "Bee.Business.TBusinessObject".</param>
        /// <param name="args">Constructor arguments.</param>
        public static object CreateInstance(string typeName, params object[] args)
        {
            return AssemblyLoader.CreateInstance(typeName, args);
        }

        /// <summary>
        /// Gets the specified attribute from a component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="attributeType">The attribute type.</param>
        public static Attribute GetAttribute(object component, Type attributeType)
        {
            return TypeDescriptor.GetAttributes(component)[attributeType];
        }

        /// <summary>
        /// Gets the specified attribute from a property of a component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="attributeType">The attribute type.</param>
        public static Attribute GetPropertyAttribute(object component, string propertyName, Type attributeType)
        {
            var property = TypeDescriptor.GetProperties(component)[propertyName];
            return property?.Attributes[attributeType];
        }

        /// <summary>
        /// Gets the value of the specified property from a component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="propertyName">The property name.</param>
        public static object GetPropertyValue(object component, string propertyName)
        {
            var property = TypeDescriptor.GetProperties(component)[propertyName];
            return property?.GetValue(component);
        }

        /// <summary>
        /// Sets the value of the specified property on a component.
        /// </summary>
        /// <param name="component">The object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The property value to set.</param>
        public static void SetPropertyValue(object component, string propertyName, object propertyValue)
        {
            TypeDescriptor.GetProperties(component)[propertyName].SetValue(component, propertyValue);
        }

        /// <summary>
        /// Determines whether the specified value is of the given generic type.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="genericType">The generic type.</param>
        public static bool IsGenericType(object value, Type genericType)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (genericType == null)
                throw new ArgumentNullException(nameof(genericType));

            Type type = value.GetType();

            // Check whether this type is the specified generic type
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                return true;

            // Check whether this type inherits from the specified generic type
            while ((type = type.BaseType) != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Verifies whether the object's type matches any of the specified types.
        /// </summary>
        /// <param name="value">The object to verify.</param>
        /// <param name="types">An array of types to check against.</param>
        public static bool CheckTypes(object value, params Type[] types)
        {
            foreach (Type type in types)
            {
                if (type.IsInstanceOfType(value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the command-line arguments as a dictionary.
        /// </summary>
        public static Dictionary<string, string> GetCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < args.Length; i++) // Skip args[0] (the executable name)
            {
                if (!args[i].StartsWith("--")) continue;

                string key = args[i].Substring(2); // Strip the "--" prefix
                // If the next argument (i + 1) exists and does not start with "-" (i.e., it's not a new option),
                // use it as the value and advance i; otherwise, default to "true" (for flag-style options like "--flag").
                string value = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[++i] : "true";

                result[key] = value;
            }
            return result;
        }

        /// <summary>
        /// Validates that multiple parameters are not null, or (if strings) not empty or whitespace-only.
        /// </summary>
        /// <param name="parameters">The collection of parameters and names to validate, as (value, paramName) tuples.</param>
        public static void EnsureNotNullOrWhiteSpace(params (object value, string paramName)[] parameters)
        {
            foreach (var p in parameters)
            {
                if (p.value == null)
                {
                    throw new ArgumentException($"{p.paramName} is required.", p.paramName);
                }

                if (p.value is string str && string.IsNullOrWhiteSpace(str))
                {
                    throw new ArgumentException($"{p.paramName} cannot be empty or whitespace.", p.paramName);
                }
            }
        }


        /// <summary>
        /// Unwraps an exception to its core cause by removing common wrapper layers.
        /// <list type="bullet">
        /// <item><description>If the exception is an <see cref="AggregateException"/>, returns the first inner exception.</description></item>
        /// <item><description>If the exception is a <see cref="System.Reflection.TargetInvocationException"/>, returns its inner exception.</description></item>
        /// <item><description>Otherwise, returns the exception itself.</description></item>
        /// </list>
        /// </summary>
        /// <param name="ex">The exception to process.</param>
        /// <returns>The innermost exception; never null.</returns>
        public static Exception UnwrapException(Exception ex)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));

            while (true)
            {
                if (ex is AggregateException aex && aex.InnerExceptions.Count > 0)
                {
                    ex = aex.Flatten().InnerExceptions[0];
                    continue;
                }
                if (ex is TargetInvocationException tie && tie.InnerException != null)
                {
                    ex = tie.InnerException;
                    continue;
                }
                return ex;
            }
        }

    }
}
