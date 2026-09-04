using System.Data;
using System.Globalization;
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
                WriteCellValue(writer, col.DefaultValue, options);
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
                WriteCellValue(writer, row[col, version], options);
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes one cell (or a column's default value).
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value to write; <c>null</c> and <see cref="DBNull"/> both write JSON null.</param>
        /// <param name="options">The serializer options, used for every other type.</param>
        /// <remarks>
        /// <para>
        /// IMPORTANT: <see cref="decimal"/>, <see cref="long"/> and <see cref="ulong"/> are written as
        /// JSON strings, not numbers — the same rule the object envelope applies, and for the same
        /// reason. A JSON number is a double to every JavaScript reader, which can hold neither a
        /// decimal's precision nor an integer past 2^53, so writing them unquoted corrupts money and
        /// identifiers before the client's own code ever sees the value. `JSON.parse` has already
        /// done the damage by then, and the column's `type` in the metadata cannot undo it.
        /// </para>
        /// <para>
        /// This used to be the half the rule did not cover: the envelope quoted them while cells —
        /// where an ERP's money actually travels — did not. The two fixtures sat next to each other
        /// in `wire-fixtures/bodies/` writing the same type two different ways.
        /// </para>
        /// <para>
        /// A column's `defaultValue` shares this path for consistency. In practice it is never one of
        /// the quoted types: `FieldDbTypeExtensions.GetDefaultValue` yields `0` (an <see cref="int"/>)
        /// for the numeric field types, so the reader — which has no column context there — is not
        /// asked to parse a quoted number back.
        /// </para>
        /// </remarks>
        private static void WriteCellValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null or DBNull:
                    writer.WriteNullValue();
                    break;
                case decimal d:
                    writer.WriteStringValue(d.ToString(CultureInfo.InvariantCulture));
                    break;
                case long l:
                    writer.WriteStringValue(l.ToString(CultureInfo.InvariantCulture));
                    break;
                case ulong u:
                    writer.WriteStringValue(u.ToString(CultureInfo.InvariantCulture));
                    break;
                default:
                    JsonSerializer.Serialize(writer, value, value.GetType(), options);
                    break;
            }
        }

        #endregion

    }
}
