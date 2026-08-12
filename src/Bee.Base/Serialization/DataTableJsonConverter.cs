using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bee.Base.Data;

namespace Bee.Base.Serialization
{
    /// <summary>
    /// Custom JSON converter for <see cref="DataTable"/> that preserves full metadata
    /// including table name, column definitions, primary keys, row state, and original/current values.
    /// </summary>
    public partial class DataTableJsonConverter : JsonConverter<DataTable>
    {
        private const string OriginalKey = "original";

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
                writer.WriteString("type", col.ResolveFieldDbType().ToString());
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
                    // WARNING: An unchanged row carries Current only. Its two versions are equal by
                    // definition, and the reader restores this state from `current` alone — it calls
                    // `AcceptChanges()`, which makes Original equal Current again. Writing both used
                    // to be justified as letting the reader "reconstruct the row state correctly",
                    // but the reader never looked at it.
                    //
                    // This is not a rare case: `DataFormRepository.GetData` calls `AcceptChanges()`
                    // before returning, so every row read from the database is Unchanged.
                    case DataRowState.Added:
                    case DataRowState.Unchanged:
                        writer.WritePropertyName("current");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Current, options);
                        break;

                    case DataRowState.Modified:
                        writer.WritePropertyName("current");
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Current, options);
                        writer.WritePropertyName(OriginalKey);
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Original, options);
                        break;

                    case DataRowState.Deleted:
                        writer.WritePropertyName(OriginalKey);
                        WriteRowValues(writer, row, value.Columns, DataRowVersion.Original, options);
                        break;
                }

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
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

    }
}
