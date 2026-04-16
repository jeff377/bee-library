using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Custom JSON converter for <see cref="DataSet"/> that preserves full metadata
    /// including dataset name, all tables, and data relations.
    /// </summary>
    public class DataSetJsonConverter : JsonConverter<DataSet>
    {
        /// <summary>
        /// Serializes a <see cref="DataSet"/> to JSON with full metadata.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, DataSet value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            // dataSetName
            writer.WriteString("dataSetName", value.DataSetName);

            // tables (delegate to DataTableJsonConverter via options)
            writer.WritePropertyName("tables");
            writer.WriteStartArray();
            foreach (DataTable table in value.Tables)
            {
                JsonSerializer.Serialize(writer, table, options);
            }
            writer.WriteEndArray();

            // relations
            writer.WritePropertyName("relations");
            writer.WriteStartArray();
            foreach (DataRelation rel in value.Relations)
            {
                writer.WriteStartObject();
                writer.WriteString("name", rel.RelationName);
                writer.WriteString("parentTable", rel.ParentTable.TableName);
                writer.WriteString("childTable", rel.ChildTable.TableName);
                writer.WritePropertyName("parentColumns");
                writer.WriteStartArray();
                foreach (var col in rel.ParentColumns)
                    writer.WriteStringValue(col.ColumnName);
                writer.WriteEndArray();
                writer.WritePropertyName("childColumns");
                writer.WriteStartArray();
                foreach (var col in rel.ChildColumns)
                    writer.WriteStringValue(col.ColumnName);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Deserializes JSON into a <see cref="DataSet"/> with full metadata restoration.
        /// </summary>
        public override DataSet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Unexpected token type '{reader.TokenType}' when reading DataSet.");

            string dataSetName = string.Empty;
            var tables = new List<DataTable>();
            var relations = new List<RelationDef>();

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
                    case "dataSetName":
                        dataSetName = reader.GetString() ?? string.Empty;
                        break;

                    case "tables":
                        tables = ReadTables(ref reader, options);
                        break;

                    case "relations":
                        relations = ReadRelations(ref reader);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            return BuildDataSet(dataSetName, tables, relations);
        }

        #region Read helpers

        private static List<DataTable> ReadTables(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var list = new List<DataTable>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var table = JsonSerializer.Deserialize<DataTable>(ref reader, options);
                    if (table != null)
                        list.Add(table);
                }
            }
            return list;
        }

        private static List<RelationDef> ReadRelations(ref Utf8JsonReader reader)
        {
            var list = new List<RelationDef>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    continue;

                var rel = new RelationDef();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName) continue;
                    var key = reader.GetString();
                    reader.Read();
                    switch (key)
                    {
                        case "name": rel.Name = reader.GetString() ?? string.Empty; break;
                        case "parentTable": rel.ParentTable = reader.GetString() ?? string.Empty; break;
                        case "childTable": rel.ChildTable = reader.GetString() ?? string.Empty; break;
                        case "parentColumns": rel.ParentColumns = ReadStringArray(ref reader); break;
                        case "childColumns": rel.ChildColumns = ReadStringArray(ref reader); break;
                    }
                }
                list.Add(rel);
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
                    list.Add(reader.GetString()!);
            }
            return list;
        }

        #endregion

        #region Build DataSet

        private static DataSet BuildDataSet(string dataSetName, List<DataTable> tables, List<RelationDef> relations)
        {
            var ds = new DataSet(dataSetName);

            foreach (var table in tables)
                ds.Tables.Add(table);

            foreach (var rel in relations)
            {
                var parentTable = ds.Tables[rel.ParentTable];
                var childTable = ds.Tables[rel.ChildTable];
                if (parentTable == null || childTable == null)
                    continue;

                var parentCols = rel.ParentColumns
                    .Select(c => parentTable.Columns[c])
                    .Where(c => c != null)
                    .ToArray();
                var childCols = rel.ChildColumns
                    .Select(c => childTable.Columns[c])
                    .Where(c => c != null)
                    .ToArray();

                if (parentCols.Length > 0 && childCols.Length > 0)
                    ds.Relations.Add(new DataRelation(rel.Name, parentCols!, childCols!));
            }

            return ds;
        }

        #endregion

        #region Internal DTO

        private sealed class RelationDef
        {
            public string Name { get; set; } = string.Empty;
            public string ParentTable { get; set; } = string.Empty;
            public string ChildTable { get; set; } = string.Empty;
            public List<string> ParentColumns { get; set; } = new();
            public List<string> ChildColumns { get; set; } = new();
        }

        #endregion
    }
}
