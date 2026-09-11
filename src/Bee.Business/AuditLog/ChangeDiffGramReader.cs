using System.Data;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Bee.Api.Contracts.AuditLog;
using Bee.Definition;
using Bee.Definition.Logging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Restores an <c>st_log_change.changes_xml</c> payload into a flat list of field-level
    /// before/after changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two payload shapes are stored, and both must stay readable: the current one written by
    /// <see cref="AuditDiffGram.Serialize"/> (an inline XSD followed by a DiffGram) and the schemaless
    /// DiffGram written before the schema was added. They are told apart by their root element, which
    /// differs for every shape the reader accepts, so no version column is needed and no stored row
    /// has to be migrated.
    /// </para>
    /// <para>
    /// The current shape carries its own schema, so it rebuilds into a real <see cref="DataSet"/> and
    /// the change detail comes from walking <see cref="DataRowVersion.Original"/> against
    /// <see cref="DataRowVersion.Current"/>. The schema comes from the payload, never from today's
    /// definitions, so a row stays readable after its form's fields have changed — the same property
    /// <see cref="SchemalessDiffGramReader"/> gets by reading element names directly.
    /// </para>
    /// </remarks>
    internal static class ChangeDiffGramReader
    {
        /// <summary>
        /// Parses a change payload into field-level changes. Returns an empty list when the payload is
        /// blank, malformed, or a minimal (non-DiffGram) delete marker — the caller still records the
        /// change event from the log row header even when no field detail is available.
        /// </summary>
        /// <param name="changesXml">The raw <c>changes_xml</c> payload.</param>
        public static List<RecordFieldChange> Read(string? changesXml)
        {
            if (string.IsNullOrWhiteSpace(changesXml)) { return []; }

            // Dispatch on the root element alone, so only the branch that is actually taken pays to
            // parse the payload. Anything that is not the current shape — the schemaless DiffGram, the
            // minimal delete marker, corrupt XML — goes to the schemaless reader, which returns an
            // empty list for the last two. That is its long-standing behaviour.
            return IsSchemaBound(changesXml)
                ? ReadSchemaBound(changesXml)
                : ReadSchemaless(changesXml);
        }

        /// <summary>
        /// Peeks at the root element to tell the current payload shape from every other one.
        /// </summary>
        private static bool IsSchemaBound(string changesXml)
        {
            try
            {
                using var stringReader = new StringReader(changesXml);
                using var reader = XmlReader.Create(stringReader, HardenedSettings());
                return reader.MoveToContent() == XmlNodeType.Element
                    && reader.NamespaceURI.Length == 0
                    && string.Equals(reader.LocalName, AuditDiffGram.RootElementName, StringComparison.Ordinal);
            }
            catch (XmlException)
            {
                return false;
            }
        }

        private static List<RecordFieldChange> ReadSchemaless(string changesXml)
        {
            XDocument doc;
            try
            {
                doc = LoadHardened(changesXml);
            }
            catch (XmlException)
            {
                // Corrupt XML carries no restorable field detail; the event header still stands alone.
                return [];
            }
            return doc.Root == null ? [] : SchemalessDiffGramReader.Read(doc.Root);
        }

        /// <summary>
        /// Rebuilds the payload into a <see cref="DataSet"/> using its own inline schema, then emits
        /// one entry per changed field.
        /// </summary>
        /// <remarks>
        /// WARNING: read the payload with one forward-only reader over the whole document, mirroring
        /// how <see cref="AuditDiffGram.Serialize"/> writes it. Handing
        /// <c>DataSet.ReadXml</c> a sub-tree reader taken from an already-parsed document
        /// (<c>XElement.CreateReader()</c>) fails on the indented payload the writer produces:
        /// <c>ReadXmlDiffgram</c> walks off the end of the sub-tree and throws
        /// <see cref="ArgumentException"/> about an empty local name. Minified input hides the
        /// problem, so a test that only covers minified payloads will not catch a regression here.
        /// </remarks>
        private static List<RecordFieldChange> ReadSchemaBound(string changesXml)
        {
            var result = new List<RecordFieldChange>();

            using var dataSet = new DataSet { Locale = CultureInfo.InvariantCulture };
            try
            {
                using var stringReader = new StringReader(changesXml);
                using var reader = XmlReader.Create(stringReader, HardenedSettings());
                reader.MoveToContent();
                // Step into the wrapper, then skip the whitespace the indented payload puts between
                // the wrapper and the inline schema, so the reader sits on the schema element.
                reader.ReadStartElement();
                if (reader.MoveToContent() != XmlNodeType.Element) { return result; }

                dataSet.ReadXmlSchema(reader);
                // A change set holds only the rows that changed, so a key or relation the schema
                // declares may legitimately have no counterpart here. Enforcing would reject a
                // payload that is perfectly valid as a record of what changed.
                dataSet.EnforceConstraints = false;
                dataSet.ReadXml(reader, XmlReadMode.DiffGram);
            }
            catch (XmlException)
            {
                return result;
            }
            catch (DataException)
            {
                // A schema the payload's own rows do not satisfy is damage, not a readable change set.
                return result;
            }

            foreach (DataTable table in dataSet.Tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    AppendRow(result, table, row);
                }
            }
            return result;
        }

        private static void AppendRow(List<RecordFieldChange> result, DataTable table, DataRow row)
        {
            switch (row.RowState)
            {
                case DataRowState.Added:
                    AppendSingleVersion(result, table, row, DataRowVersion.Current, ChangeKind.Insert);
                    break;
                case DataRowState.Deleted:
                    AppendSingleVersion(result, table, row, DataRowVersion.Original, ChangeKind.Delete);
                    break;
                case DataRowState.Modified:
                    AppendModified(result, table, row);
                    break;
                default:
                    // Unchanged / Detached rows carry no change detail. GetChanges does not produce
                    // them, so this is defensive rather than expected.
                    break;
            }
        }

        /// <summary>
        /// Emits every non-key column of a row that exists in only one version — an insert (current
        /// values, no old value) or a delete (original values, no new value).
        /// </summary>
        private static void AppendSingleVersion(List<RecordFieldChange> result, DataTable table, DataRow row,
            DataRowVersion version, ChangeKind kind)
        {
            var rowKey = GetRowKey(table, row, version);
            foreach (DataColumn column in table.Columns)
            {
                if (IsRowKeyColumn(column.ColumnName)) { continue; }
                var value = ToText(row[column, version]);
                result.Add(kind == ChangeKind.Insert
                    ? Field(table.TableName, rowKey, kind, column.ColumnName, null, value)
                    : Field(table.TableName, rowKey, kind, column.ColumnName, value, null));
            }
        }

        /// <summary>
        /// Emits only the columns whose value actually changed, comparing the row's two versions.
        /// </summary>
        private static void AppendModified(List<RecordFieldChange> result, DataTable table, DataRow row)
        {
            var rowKey = GetRowKey(table, row, DataRowVersion.Current);
            foreach (DataColumn column in table.Columns)
            {
                if (IsRowKeyColumn(column.ColumnName)) { continue; }
                var oldValue = ToText(row[column, DataRowVersion.Original]);
                var newValue = ToText(row[column, DataRowVersion.Current]);
                if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                {
                    result.Add(Field(table.TableName, rowKey, ChangeKind.Update, column.ColumnName, oldValue, newValue));
                }
            }
        }

        private static string? GetRowKey(DataTable table, DataRow row, DataRowVersion version)
            => table.Columns.Contains(SysFields.RowId) ? ToText(row[SysFields.RowId, version]) : null;

        private static bool IsRowKeyColumn(string columnName)
            => string.Equals(columnName, SysFields.RowId, StringComparison.Ordinal);

        /// <summary>
        /// Renders a restored value the way the DiffGram spelled it, so a change reads identically
        /// whichever payload shape it was stored in.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: use <see cref="XmlConvert"/>, not <c>ToString()</c> or
        /// <c>ValueUtilities.CStr</c>. Those are culture-sensitive, which would both diverge from the
        /// schemaless reader (which returns the payload's own XML text) and make a stored value render
        /// differently on a server whose culture happens to differ — the very thing the write side
        /// avoids by building its tables with an invariant locale.
        /// <para>
        /// NOTE: agreement with the schemaless reader on <see cref="DateTime"/> columns also depends on
        /// <c>Bee.Base.Data.DataTableExtensions.AddColumn</c> setting
        /// <see cref="DataSetDateTime.Unspecified"/>. A <see cref="DataTable"/> built by hand defaults
        /// to <c>UnspecifiedLocal</c>, whose DiffGram carries a timezone offset that the restored value
        /// does not — so the two shapes would disagree on that column.
        /// </para>
        /// </remarks>
        private static string? ToText(object? value)
        {
            if (value == null || value == DBNull.Value) { return null; }
            return value switch
            {
                string text => text,
                DateTime dateTime => XmlConvert.ToString(dateTime, XmlDateTimeSerializationMode.RoundtripKind),
                DateTimeOffset dateTimeOffset => XmlConvert.ToString(dateTimeOffset),
                decimal number => XmlConvert.ToString(number),
                bool flag => XmlConvert.ToString(flag),
                Guid guid => XmlConvert.ToString(guid),
                int number => XmlConvert.ToString(number),
                long number => XmlConvert.ToString(number),
                short number => XmlConvert.ToString(number),
                byte number => XmlConvert.ToString(number),
                double number => XmlConvert.ToString(number),
                float number => XmlConvert.ToString(number),
                TimeSpan span => XmlConvert.ToString(span),
                byte[] bytes => Convert.ToBase64String(bytes),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture),
            };
        }

        private static RecordFieldChange Field(string tableName, string? rowKey, ChangeKind rowState,
            string fieldName, string? oldValue, string? newValue)
            => new RecordFieldChange
            {
                TableName = tableName,
                RowKey = rowKey,
                RowState = rowState,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
            };

        /// <summary>
        /// Hardens against XXE (scanning.md): no DTD, no external entity resolution. Every reader over
        /// a stored payload is built from this, including the one handed to <c>DataSet.ReadXml</c>.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: <see cref="XmlReaderSettings.CheckCharacters"/> is off because
        /// <see cref="AuditDiffGram.Serialize"/> writes characters XML 1.0 forbids as character
        /// references rather than failing a committed save over them. With the check on, such a
        /// payload reads as corrupt and restores no field detail. The XXE hardening is carried by the
        /// DTD and resolver settings and does not depend on this one.
        /// </remarks>
        private static XmlReaderSettings HardenedSettings()
            => new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, CheckCharacters = false };

        private static XDocument LoadHardened(string xml)
        {
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, HardenedSettings());
            return XDocument.Load(reader);
        }
    }
}
