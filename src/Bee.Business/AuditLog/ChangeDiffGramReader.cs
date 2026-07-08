using System.Xml;
using System.Xml.Linq;
using Bee.Api.Contracts;
using Bee.Definition;
using Bee.Definition.Logging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Restores an <c>st_log_change.changes_xml</c> payload — a schemaless DataSet DiffGram written by
    /// the Save/Delete audit path — into a flat list of field-level before/after changes.
    /// </summary>
    /// <remarks>
    /// The DiffGram is parsed directly with <see cref="XDocument"/> rather than
    /// <c>DataSet.ReadXml</c>: the write side emits a DiffGram <b>without</b> an inline schema, which
    /// <c>ReadXml</c> cannot reconstruct into a fresh <c>DataSet</c> (it yields zero tables). Direct
    /// XML parsing also keeps the restore off the
    /// <c>XmlSerializer</c> reflection path, so it is unaffected by the trim / AOT concerns of ADR-025.
    /// </remarks>
    internal static class ChangeDiffGramReader
    {
        private const string DiffgrNs = "urn:schemas-microsoft-com:xml-diffgram-v1";

        /// <summary>
        /// Parses a DiffGram payload into field-level changes. Returns an empty list when the payload is
        /// blank, malformed, or a minimal (non-DiffGram) delete marker — the caller still records the
        /// change event from the log row header even when no field detail is available.
        /// </summary>
        /// <param name="changesXml">The raw <c>changes_xml</c> DiffGram.</param>
        public static List<RecordFieldChange> Read(string? changesXml)
        {
            var result = new List<RecordFieldChange>();
            if (string.IsNullOrWhiteSpace(changesXml)) { return result; }

            XDocument doc;
            try
            {
                doc = LoadHardened(changesXml);
            }
            catch (XmlException)
            {
                // A non-DiffGram payload (e.g. the minimal delete marker) or corrupt XML carries no
                // restorable field detail; the event header still stands on its own.
                return result;
            }

            var root = doc.Root;
            if (root == null) { return result; }

            XNamespace diff = DiffgrNs;
            var dataBlock = root.Elements().FirstOrDefault(e => e.Name.Namespace != diff);
            var beforeBlock = root.Elements(diff + "before").FirstOrDefault();

            // Index the before-image rows by their diffgr:id so modified rows can be paired with their
            // originals and any unpaired before-row can be recognised as a delete.
            var beforeById = new Dictionary<string, XElement>(StringComparer.Ordinal);
            if (beforeBlock != null)
            {
                foreach (var row in beforeBlock.Elements())
                {
                    var id = row.Attribute(diff + "id")?.Value;
                    if (id != null) { beforeById[id] = row; }
                }
            }

            var matchedBeforeIds = new HashSet<string>(StringComparer.Ordinal);

            if (dataBlock != null)
            {
                foreach (var row in dataBlock.Elements())
                {
                    AppendCurrentRow(result, diff, row, beforeById, matchedBeforeIds);
                }
            }

            // Before-rows with no matching current row are deletes: emit their before-image.
            foreach (var pair in beforeById)
            {
                if (matchedBeforeIds.Contains(pair.Key)) { continue; }
                var row = pair.Value;
                var before = ReadColumns(row);
                var rowKey = GetRowKey(before);
                foreach (var column in before)
                {
                    if (IsRowKeyColumn(column.Key)) { continue; }
                    result.Add(Field(row.Name.LocalName, rowKey, ChangeKind.Delete, column.Key, column.Value, null));
                }
            }

            return result;
        }

        private static void AppendCurrentRow(List<RecordFieldChange> result, XNamespace diff, XElement row,
            Dictionary<string, XElement> beforeById, HashSet<string> matchedBeforeIds)
        {
            var tableName = row.Name.LocalName;
            var hasChanges = row.Attribute(diff + "hasChanges")?.Value;
            var current = ReadColumns(row);
            var rowKey = GetRowKey(current);

            if (string.Equals(hasChanges, "inserted", StringComparison.Ordinal))
            {
                foreach (var column in current)
                {
                    if (IsRowKeyColumn(column.Key)) { continue; }
                    result.Add(Field(tableName, rowKey, ChangeKind.Insert, column.Key, null, column.Value));
                }
                return;
            }

            // Modified: pair with the before-image (via diffgr:id) and emit only columns that differ.
            var before = new Dictionary<string, string?>(StringComparer.Ordinal);
            var id = row.Attribute(diff + "id")?.Value;
            if (id != null && beforeById.TryGetValue(id, out var beforeRow))
            {
                matchedBeforeIds.Add(id);
                before = ReadColumns(beforeRow);
            }

            // Union the column names so a value set to (or from) null — where the DiffGram omits the
            // element on one side — is still captured.
            var names = new HashSet<string>(before.Keys, StringComparer.Ordinal);
            names.UnionWith(current.Keys);
            foreach (var name in names)
            {
                if (IsRowKeyColumn(name)) { continue; }
                before.TryGetValue(name, out var oldValue);
                current.TryGetValue(name, out var newValue);
                if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                {
                    result.Add(Field(tableName, rowKey, ChangeKind.Update, name, oldValue, newValue));
                }
            }
        }

        private static Dictionary<string, string?> ReadColumns(XElement row)
        {
            var columns = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var column in row.Elements())
            {
                columns[column.Name.LocalName] = column.Value;
            }
            return columns;
        }

        private static string? GetRowKey(Dictionary<string, string?> columns)
            => columns.TryGetValue(SysFields.RowId, out var value) ? value : null;

        private static bool IsRowKeyColumn(string columnName)
            => string.Equals(columnName, SysFields.RowId, StringComparison.Ordinal);

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

        private static XDocument LoadHardened(string xml)
        {
            // Harden against XXE (scanning.md): no DTD, no external entity resolution.
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, settings);
            return XDocument.Load(reader);
        }
    }
}
