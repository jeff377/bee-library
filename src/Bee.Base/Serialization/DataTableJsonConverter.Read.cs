using System.Data;
using System.Globalization;
using System.Text.Json;
using Bee.Base.Data;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Reading the JSON back into the intermediate column/row shapes.
    /// </summary>
    /// <remarks>
    /// The mirror of `Write` in the main file. Any change here needs the writer checked in the
    /// same breath: the two halves define one format, and an asymmetry between them is silent
    /// — a reader that ignores a field the writer emits costs nothing at runtime and loses data.
    /// </remarks>
    public partial class DataTableJsonConverter
    {
        /// <summary>
        /// Deserializes JSON into a <see cref="DataTable"/> with full metadata restoration.
        /// </summary>
        public override DataTable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Unexpected token type '{reader.TokenType}' when reading DataTable.");

            string tableName = string.Empty;
            var columns = new List<ColumnDef>();
            var primaryKeys = new List<string>();
            var rows = new List<RowDef>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propName = reader.GetString();
                reader.Read();

                switch (propName)
                {
                    case "tableName":
                        tableName = reader.GetString() ?? string.Empty;
                        break;

                    case "columns":
                        columns = ReadColumns(ref reader);
                        break;

                    case "primaryKeys":
                        primaryKeys = ReadStringArray(ref reader);
                        break;

                    case "rows":
                        rows = ReadRows(ref reader, columns);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            return BuildDataTable(tableName, columns, primaryKeys, rows);
        }


        #region Read helpers

        private static List<ColumnDef> ReadColumns(ref Utf8JsonReader reader)
        {
            var list = new List<ColumnDef>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    continue;

                var col = new ColumnDef();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var key = reader.GetString();
                    reader.Read();
                    ReadColumnField(ref reader, col, key);
                }
                list.Add(col);
            }
            return list;
        }

        private static void ReadColumnField(ref Utf8JsonReader reader, ColumnDef col, string? key)
        {
            switch (key)
            {
                case "name": col.Name = reader.GetString() ?? string.Empty; break;
                case "type": col.FieldType = Enum.Parse<FieldDbType>(reader.GetString() ?? "String"); break;
                case "allowNull": col.AllowNull = reader.GetBoolean(); break;
                case "readOnly": col.ReadOnly = reader.GetBoolean(); break;
                case "maxLength": col.MaxLength = reader.GetInt32(); break;
                case "caption": col.Caption = reader.GetString() ?? string.Empty; break;
                case "defaultValue": col.DefaultValue = reader.TokenType == JsonTokenType.Null ? null : ReadPrimitiveValue(ref reader); break;
            }
        }

        /// <summary>
        /// Parses a quoted numeric cell back to the column's own type.
        /// </summary>
        /// <param name="text">The quoted text.</param>
        /// <param name="targetType">The column's CLR type — <see cref="decimal"/>, <see cref="long"/> or <see cref="ulong"/>.</param>
        /// <param name="value">The parsed value when this returns <c>true</c>.</param>
        /// <remarks>
        /// Returns <c>false</c> rather than throwing when the text is not a number: the caller then
        /// hands the string on unchanged and <see cref="DataRow"/> reports the type mismatch against
        /// the real column. Swallowing it here would turn a malformed payload into a silent default.
        /// </remarks>
        private static bool TryParseQuotedNumber(string text, Type targetType, out object? value)
        {
            if (targetType == typeof(decimal) && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            {
                value = d;
                return true;
            }
            if (targetType == typeof(long) && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            {
                value = l;
                return true;
            }
            if (targetType == typeof(ulong) && ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var u))
            {
                value = u;
                return true;
            }
            value = null;
            return false;
        }

        private static List<string> ReadStringArray(ref Utf8JsonReader reader)
        {
            var list = new List<string>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                    list.Add(reader.GetString()!);
            }
            return list;
        }

        private static List<RowDef> ReadRows(ref Utf8JsonReader reader, List<ColumnDef> columns)
        {
            var list = new List<RowDef>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            // Build type lookup for value conversion
            var typeLookup = columns.ToDictionary(c => c.Name, c => DbTypeConverter.ToType(c.FieldType));

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    continue;

                var rowDef = new RowDef();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var key = reader.GetString();
                    reader.Read();
                    switch (key)
                    {
                        case "state":
                            rowDef.State = Enum.Parse<DataRowState>(reader.GetString() ?? "Added");
                            break;
                        case "current":
                            rowDef.CurrentValues = ReadValueMap(ref reader, typeLookup);
                            break;
                        case OriginalKey:
                            rowDef.OriginalValues = ReadValueMap(ref reader, typeLookup);
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                list.Add(rowDef);
            }
            return list;
        }

        private static Dictionary<string, object?> ReadValueMap(ref Utf8JsonReader reader, Dictionary<string, Type> typeLookup)
        {
            var map = new Dictionary<string, object?>();
            if (reader.TokenType != JsonTokenType.StartObject)
                return map;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var colName = reader.GetString()!;
                reader.Read();

                if (reader.TokenType == JsonTokenType.Null)
                {
                    map[colName] = null;
                }
                else
                {
                    typeLookup.TryGetValue(colName, out var targetType);
                    var rawValue = ReadPrimitiveValue(ref reader, targetType);
                    if (rawValue != null && targetType != null)
                        map[colName] = ConvertValue(rawValue, targetType);
                    else
                        map[colName] = rawValue;
                }
            }
            return map;
        }

        /// <summary>
        /// Reads a primitive JSON value from the reader and returns it as a .NET object.
        /// </summary>
        /// <param name="reader">The JSON reader positioned on the value.</param>
        /// <param name="targetType">
        /// The CLR type of the destination column when known; <c>null</c> for contexts with no
        /// column (such as a column's own <c>defaultValue</c>).
        /// </param>
        private static object? ReadPrimitiveValue(ref Utf8JsonReader reader, Type? targetType = null)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    // The writer quotes decimal / long / ulong so a JavaScript reader does not lose
                    // them to double. Parse them back against the column's own type — invariant,
                    // because that is how they were written.
                    if (targetType == typeof(decimal) || targetType == typeof(long) || targetType == typeof(ulong))
                    {
                        var text = reader.GetString();
                        if (!string.IsNullOrEmpty(text) && TryParseQuotedNumber(text, targetType, out var number))
                            return number;
                        return text;
                    }
                    // Only guess at a date when the column is not a string one. A text column
                    // holding an ISO-8601-shaped value ("2026-07-28" in a remark or a user-defined
                    // code) would otherwise be parsed to DateTime and then rendered back as
                    // "07/28/2026 00:00:00" — a different value, and long enough to breach
                    // DataColumn.MaxLength.
                    if (targetType != typeof(string) && reader.TryGetDateTime(out var dt))
                        return dt;
                    return reader.GetString();
                case JsonTokenType.Number:
                    // Still accepted: a client written against a release that wrote these unquoted
                    // keeps working. Only the writer changed.
                    if (reader.TryGetInt64(out var l))
                        return l;
                    // Decimal before double: a monetary value with more than 15 significant digits
                    // loses precision through double, which would break the round-then-sum rule
                    // that detail lines must add up to the stated total.
                    if (reader.TryGetDecimal(out var dec))
                        return dec;
                    return reader.GetDouble();
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Null:
                    return null;
                default:
                    // For complex tokens, skip and return null
                    reader.Skip();
                    return null;
            }
        }

        /// <summary>
        /// Converts a deserialized JSON value to the target column type.
        /// JSON numbers may deserialize as long/double; this ensures correct .NET types.
        /// </summary>
        internal static object ConvertValue(object value, Type targetType)
        {
            if (targetType == typeof(byte[]))
            {
                // JSON stores byte[] as Base64 string
                if (value is string base64)
                    return Convert.FromBase64String(base64);
                return value;
            }

            if (targetType == typeof(Guid))
            {
                if (value is string guidStr)
                    return Guid.Parse(guidStr);
                return value;
            }

            if (targetType == typeof(DateTime))
            {
                if (value is DateTime dt)
                    return dt;
                if (value is string dtStr)
                    return DateTime.Parse(dtStr, CultureInfo.InvariantCulture);
                return value;
            }

            // Numeric type coercion (JSON long → int/short/decimal etc.)
            // A value the target type cannot represent is handed back as-is rather than failing the
            // whole payload — these are the conversion failures that means. Anything else is a bug.
            try
            {
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
            {
                return value;
            }
        }

        #endregion
    }
}
