using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bee.Base.Data;
using Newtonsoft.Json;

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
        public override void WriteJson(JsonWriter writer, DataTable value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            // tableName
            writer.WritePropertyName("tableName");
            writer.WriteValue(value.TableName);

            // columns
            writer.WritePropertyName("columns");
            writer.WriteStartArray();
            foreach (DataColumn col in value.Columns)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("name");
                writer.WriteValue(col.ColumnName);
                writer.WritePropertyName("type");
                writer.WriteValue(DbTypeConverter.ToFieldDbType(col.DataType).ToString());
                writer.WritePropertyName("allowNull");
                writer.WriteValue(col.AllowDBNull);
                writer.WritePropertyName("readOnly");
                writer.WriteValue(col.ReadOnly);
                writer.WritePropertyName("maxLength");
                writer.WriteValue(col.MaxLength);
                writer.WritePropertyName("caption");
                writer.WriteValue(col.Caption);
                writer.WritePropertyName("defaultValue");
                if (col.DefaultValue is DBNull || col.DefaultValue == null)
                    writer.WriteNull();
                else
                    serializer.Serialize(writer, col.DefaultValue);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            // primaryKeys
            writer.WritePropertyName("primaryKeys");
            writer.WriteStartArray();
            foreach (var pk in value.PrimaryKey)
                writer.WriteValue(pk.ColumnName);
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
                writer.WritePropertyName("state");
                writer.WriteValue(state.ToString());

                switch (state)
                {
                    case DataRowState.Added:
                    case DataRowState.Unchanged:
                        writer.WritePropertyName("current");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Current, serializer);
                        if (state == DataRowState.Unchanged)
                        {
                            // For Unchanged rows, original == current; write original explicitly
                            // so the reader can reconstruct the row state correctly.
                            writer.WritePropertyName("original");
                            WriteRowValues(writer, row, value.Columns, DataRowVersion.Original, serializer);
                        }
                        break;

                    case DataRowState.Modified:
                        writer.WritePropertyName("current");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Current, serializer);
                        writer.WritePropertyName("original");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Original, serializer);
                        break;

                    case DataRowState.Deleted:
                        writer.WritePropertyName("original");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Original, serializer);
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
        public override DataTable ReadJson(JsonReader reader, Type objectType, DataTable existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Unexpected token type '{reader.TokenType}' when reading DataTable.");

            string tableName = string.Empty;
            var columns = new List<ColumnDef>();
            var primaryKeys = new List<string>();
            var rows = new List<RowDef>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                    continue;

                var propName = (string)reader.Value;
                reader.Read();

                switch (propName)
                {
                    case "tableName":
                        tableName = reader.Value?.ToString() ?? string.Empty;
                        break;

                    case "columns":
                        columns = ReadColumns(reader);
                        break;

                    case "primaryKeys":
                        primaryKeys = ReadStringArray(reader);
                        break;

                    case "rows":
                        rows = ReadRows(reader, columns);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            return BuildDataTable(tableName, columns, primaryKeys, rows);
        }

        #region Write helpers

        private static void WriteRowValues(JsonWriter writer, DataRow row, DataColumnCollection columns, DataRowVersion version, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (DataColumn col in columns)
            {
                writer.WritePropertyName(col.ColumnName);
                var val = row[col, version];
                if (val is DBNull)
                    writer.WriteNull();
                else
                    serializer.Serialize(writer, val);
            }
            writer.WriteEndObject();
        }

        #endregion

        #region Read helpers

        private static List<ColumnDef> ReadColumns(JsonReader reader)
        {
            var list = new List<ColumnDef>();
            if (reader.TokenType != JsonToken.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                if (reader.TokenType != JsonToken.StartObject)
                    continue;

                var col = new ColumnDef();
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType != JsonToken.PropertyName) continue;
                    var key = (string)reader.Value;
                    reader.Read();
                    switch (key)
                    {
                        case "name": col.Name = reader.Value?.ToString() ?? string.Empty; break;
                        case "type": col.FieldType = Enum.Parse<FieldDbType>(reader.Value?.ToString() ?? "String"); break;
                        case "allowNull": col.AllowNull = Convert.ToBoolean(reader.Value); break;
                        case "readOnly": col.ReadOnly = Convert.ToBoolean(reader.Value); break;
                        case "maxLength": col.MaxLength = Convert.ToInt32(reader.Value); break;
                        case "caption": col.Caption = reader.Value?.ToString() ?? string.Empty; break;
                        case "defaultValue": col.DefaultValue = reader.TokenType == JsonToken.Null ? null : reader.Value; break;
                    }
                }
                list.Add(col);
            }
            return list;
        }

        private static List<string> ReadStringArray(JsonReader reader)
        {
            var list = new List<string>();
            if (reader.TokenType != JsonToken.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                if (reader.Value != null)
                    list.Add(reader.Value.ToString());
            }
            return list;
        }

        private static List<RowDef> ReadRows(JsonReader reader, List<ColumnDef> columns)
        {
            var list = new List<RowDef>();
            if (reader.TokenType != JsonToken.StartArray)
                return list;

            // Build type lookup for value conversion
            var typeLookup = columns.ToDictionary(c => c.Name, c => DbTypeConverter.ToType(c.FieldType));

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                if (reader.TokenType != JsonToken.StartObject)
                    continue;

                var rowDef = new RowDef();
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType != JsonToken.PropertyName) continue;
                    var key = (string)reader.Value;
                    reader.Read();
                    switch (key)
                    {
                        case "state":
                            rowDef.State = Enum.Parse<DataRowState>(reader.Value?.ToString() ?? "Added");
                            break;
                        case "current":
                            rowDef.CurrentValues = ReadValueMap(reader, typeLookup);
                            break;
                        case "original":
                            rowDef.OriginalValues = ReadValueMap(reader, typeLookup);
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

        private static Dictionary<string, object> ReadValueMap(JsonReader reader, Dictionary<string, Type> typeLookup)
        {
            var map = new Dictionary<string, object>();
            if (reader.TokenType != JsonToken.StartObject)
                return map;

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (reader.TokenType != JsonToken.PropertyName) continue;
                var colName = (string)reader.Value;
                reader.Read();

                if (reader.TokenType == JsonToken.Null)
                {
                    map[colName] = null;
                }
                else
                {
                    var rawValue = reader.Value;
                    if (rawValue != null && typeLookup.TryGetValue(colName, out var targetType))
                        map[colName] = ConvertValue(rawValue, targetType);
                    else
                        map[colName] = rawValue;
                }
            }
            return map;
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
