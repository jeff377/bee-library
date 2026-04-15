using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;

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
        public override void WriteJson(JsonWriter writer, DataSet value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            // dataSetName
            writer.WritePropertyName("dataSetName");
            writer.WriteValue(value.DataSetName);

            // tables (delegate to DataTableJsonConverter via serializer)
            writer.WritePropertyName("tables");
            writer.WriteStartArray();
            foreach (DataTable table in value.Tables)
            {
                serializer.Serialize(writer, table);
            }
            writer.WriteEndArray();

            // relations
            writer.WritePropertyName("relations");
            writer.WriteStartArray();
            foreach (DataRelation rel in value.Relations)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("name");
                writer.WriteValue(rel.RelationName);
                writer.WritePropertyName("parentTable");
                writer.WriteValue(rel.ParentTable.TableName);
                writer.WritePropertyName("childTable");
                writer.WriteValue(rel.ChildTable.TableName);
                writer.WritePropertyName("parentColumns");
                writer.WriteStartArray();
                foreach (var col in rel.ParentColumns)
                    writer.WriteValue(col.ColumnName);
                writer.WriteEndArray();
                writer.WritePropertyName("childColumns");
                writer.WriteStartArray();
                foreach (var col in rel.ChildColumns)
                    writer.WriteValue(col.ColumnName);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Deserializes JSON into a <see cref="DataSet"/> with full metadata restoration.
        /// </summary>
        public override DataSet ReadJson(JsonReader reader, Type objectType, DataSet existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException($"Unexpected token type '{reader.TokenType}' when reading DataSet.");

            string dataSetName = string.Empty;
            var tables = new List<DataTable>();
            var relations = new List<RelationDef>();

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
                    case "dataSetName":
                        dataSetName = reader.Value?.ToString() ?? string.Empty;
                        break;

                    case "tables":
                        tables = ReadTables(reader, serializer);
                        break;

                    case "relations":
                        relations = ReadRelations(reader);
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            return BuildDataSet(dataSetName, tables, relations);
        }

        #region Read helpers

        private static List<DataTable> ReadTables(JsonReader reader, JsonSerializer serializer)
        {
            var list = new List<DataTable>();
            if (reader.TokenType != JsonToken.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var table = serializer.Deserialize<DataTable>(reader);
                    if (table != null)
                        list.Add(table);
                }
            }
            return list;
        }

        private static List<RelationDef> ReadRelations(JsonReader reader)
        {
            var list = new List<RelationDef>();
            if (reader.TokenType != JsonToken.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                if (reader.TokenType != JsonToken.StartObject)
                    continue;

                var rel = new RelationDef();
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType != JsonToken.PropertyName) continue;
                    var key = (string)reader.Value;
                    reader.Read();
                    switch (key)
                    {
                        case "name": rel.Name = reader.Value?.ToString() ?? string.Empty; break;
                        case "parentTable": rel.ParentTable = reader.Value?.ToString() ?? string.Empty; break;
                        case "childTable": rel.ChildTable = reader.Value?.ToString() ?? string.Empty; break;
                        case "parentColumns": rel.ParentColumns = ReadStringArray(reader); break;
                        case "childColumns": rel.ChildColumns = ReadStringArray(reader); break;
                    }
                }
                list.Add(rel);
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
                    list.Add(reader.Value.ToString()!);
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
