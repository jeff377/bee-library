using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bee.Base.Data;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Custom JSON converter for <see cref="DataTable"/> that preserves full metadata
    /// including table name, column definitions, primary keys, row state, and original/current values.
    /// </summary>
    public class DataTableJsonConverter : JsonConverter<DataTable>
    {
        /// <summary>
        /// Serializes a <see cref="DataTable"/> to JSON with full metadata.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, DataTable value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            // tableName
            writer.WriteString("tableName", value.TableName);

            // columns
            writer.WritePropertyName("columns");
            writer.WriteStartArray();
            foreach (DataColumn col in value.Columns)
            {
                writer.WriteStartObject();
                writer.WriteString("name", col.ColumnName);
                writer.WriteString("type", DbTypeConverter.ToFieldDbType(col.DataType).ToString());
                writer.WriteBoolean("allowNull", col.AllowDBNull);
                writer.WriteBoolean("readOnly", col.ReadOnly);
                writer.WriteNumber("maxLength", col.MaxLength);
                writer.WriteString("caption", col.Caption);
                writer.WritePropertyName("defaultValue");
                if (col.DefaultValue is DBNull || col.DefaultValue == null)
                    writer.WriteNullValue();
                else
                    JsonSerializer.Serialize(writer, col.DefaultValue, col.DefaultValue.GetType(), options);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            // primaryKeys
            writer.WritePropertyName("primaryKeys");
            writer.WriteStartArray();
            foreach (var pk in value.PrimaryKey)
                writer.WriteStringValue(pk.ColumnName);
            writer.WriteEndArray();

            // rows
            writer.WritePropertyName("rows");
            writer.WriteStartArray();
            foreach (DataRow row in value.Rows)
            {
                var state = row.RowState;
                if (state == DataRowState.Detached)
                    continue;

                writer.WriteStartObject();
                writer.WriteString("state", state.ToString());

                switch (state)
                {
                    case DataRowState.Added:
                    case DataRowState.Unchanged:
                        writer.WritePropertyName("current");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Current, options);
                        if (state == DataRowState.Unchanged)
                        {
                            // For Unchanged rows, original == current; write original explicitly
                            // so the reader can reconstruct the row state correctly.
                            writer.WritePropertyName("original");
                            WriteRowValues(writer, row, value.Columns, DataRowVersion.Original, options);
                        }
                        break;

                    case DataRowState.Modified:
                        writer.WritePropertyName("current");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Current, options);
                        writer.WritePropertyName("original");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Original, options);
                        break;

                    case DataRowState.Deleted:
                        writer.WritePropertyName("original");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Original, options);
                        break;
                }

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Deserializes JSON into a <see cref="DataTable"/> with full metadata restoration.
        /// </summary>
        public override DataTable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        #region Write helpers

        private static void WriteRowValues(Utf8JsonWriter writer, DataRow row, DataColumnCollection columns, DataRowVersion version, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (DataColumn col in columns)
            {
                writer.WritePropertyName(col.ColumnName);
                var val = row[col, version];
                if (val is DBNull)
                    writer.WriteNullValue();
                else
                    JsonSerializer.Serialize(writer, val, val.GetType(), options);
            }
            writer.WriteEndObject();
        }

        #endregion

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
                list.Add(col);
            }
            return list;
        }

        private static List<string> ReadStringArray(ref Utf8JsonReader reader)
        {
            var list = new List<string>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                    list.Add(reader.GetString());
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
                        case "original":
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

        private static Dictionary<string, object> ReadValueMap(ref Utf8JsonReader reader, Dictionary<string, Type> typeLookup)
        {
            var map = new Dictionary<string, object>();
            if (reader.TokenType != JsonTokenType.StartObject)
                return map;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var colName = reader.GetString();
                reader.Read();

                if (reader.TokenType == JsonTokenType.Null)
                {
                    map[colName] = null;
                }
                else
                {
                    var rawValue = ReadPrimitiveValue(ref reader);
                    if (rawValue != null && typeLookup.TryGetValue(colName, out var targetType))
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
        private static object ReadPrimitiveValue(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    // Try DateTime first, then fall back to string
                    if (reader.TryGetDateTime(out var dt))
                        return dt;
                    return reader.GetString();
                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var l))
                        return l;
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
                    return DateTime.Parse(dtStr);
                return value;
            }

            // Numeric type coercion (JSON long → int/short/decimal etc.)
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value;
            }
        }

        #endregion

        #region Build DataTable

        private static DataTable BuildDataTable(string tableName, List<ColumnDef> columns, List<string> primaryKeys, List<RowDef> rows)
        {
            var dt = new DataTable(tableName);

            // Build columns
            foreach (var col in columns)
            {
                var netType = DbTypeConverter.ToType(col.FieldType);
                var dc = new DataColumn(col.Name, netType)
                {
                    AllowDBNull = col.AllowNull,
                    ReadOnly = col.ReadOnly,
                    MaxLength = col.MaxLength,
                    Caption = col.Caption,
                    DefaultValue = col.DefaultValue ?? DBNull.Value
                };
                dt.Columns.Add(dc);
            }

            // Set primary keys
            if (primaryKeys.Count > 0)
            {
                var pkCols = primaryKeys
                    .Where(pk => dt.Columns.Contains(pk))
                    .Select(pk => dt.Columns[pk])
                    .ToArray();
                if (pkCols.Length > 0)
                    dt.PrimaryKey = pkCols;
            }

            // Restore rows (same logic as SerializableDataTable.ToDataTable)
            foreach (var rowDef in rows)
            {
                var row = dt.NewRow();

                switch (rowDef.State)
                {
                    case DataRowState.Unchanged:
                        SetRowValues(row, rowDef.CurrentValues);
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        break;

                    case DataRowState.Added:
                        SetRowValues(row, rowDef.CurrentValues);
                        dt.Rows.Add(row);
                        break;

                    case DataRowState.Modified:
                        // Write original values first
                        SetRowValues(row, rowDef.OriginalValues);
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        // Overwrite with current values
                        SetRowValues(row, rowDef.CurrentValues);
                        break;

                    case DataRowState.Deleted:
                        SetRowValues(row, rowDef.OriginalValues);
                        dt.Rows.Add(row);
                        row.AcceptChanges();
                        row.Delete();
                        break;
                }
            }

            return dt;
        }

        private static void SetRowValues(DataRow row, Dictionary<string, object> values)
        {
            if (values == null) return;
            foreach (var kvp in values)
            {
                row[kvp.Key] = kvp.Value ?? DBNull.Value;
            }
        }

        #endregion

        #region Internal DTOs

        private sealed class ColumnDef
        {
            public string Name { get; set; } = string.Empty;
            public FieldDbType FieldType { get; set; } = FieldDbType.String;
            public bool AllowNull { get; set; } = true;
            public bool ReadOnly { get; set; }
            public int MaxLength { get; set; } = -1;
            public string Caption { get; set; } = string.Empty;
            public object DefaultValue { get; set; }
        }

        private sealed class RowDef
        {
            public DataRowState State { get; set; } = DataRowState.Added;
            public Dictionary<string, object> CurrentValues { get; set; }
            public Dictionary<string, object> OriginalValues { get; set; }
        }

        #endregion
    }
}
