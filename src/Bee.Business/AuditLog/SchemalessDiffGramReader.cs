using System.Xml.Linq;
using Bee.Api.Contracts.AuditLog;
using Bee.Definition;
using Bee.Definition.Logging;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Reads the <b>schemaless</b> <c>st_log_change.changes_xml</c> payload — a bare DiffGram with no
    /// inline schema, which is what the write side emitted before the schema was added.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is <b>frozen</b>. It exists to keep rows written by the earlier format readable, and
    /// nothing writes that format any more — see <see cref="AuditDiffGram.Serialize"/>. Change it only
    /// to fix a defect in reading those rows; new behaviour belongs in
    /// <see cref="ChangeDiffGramReader"/>.
    /// </para>
    /// <para>
    /// The DiffGram is parsed directly with <see cref="XDocument"/> because a payload without an
    /// inline schema cannot be reconstructed by <c>DataSet.ReadXml</c> into a fresh <c>DataSet</c> —
    /// it yields zero tables. Reading the payload's own element names rather than replaying today's
    /// schema over it is also what keeps a row readable after its form's fields have changed.
    /// </para>
    /// </remarks>
    internal static class SchemalessDiffGramReader
    {
        private const string DiffgrNs = "urn:schemas-microsoft-com:xml-diffgram-v1";

        /// <summary>
        /// Parses a schemaless DiffGram into field-level changes.
        /// </summary>
        /// <param name="root">The payload's root element (the <c>diffgr:diffgram</c> element).</param>
        public static List<RecordFieldChange> Read(XElement root)
        {
            var result = new List<RecordFieldChange>();

            XNamespace diff = DiffgrNs;
            var dataBlock = root.Elements().FirstOrDefault(e => e.Name.Namespace != diff);
            var beforeBlock = root.Elements(diff + "before").FirstOrDefault();

            var beforeById = IndexBeforeRows(beforeBlock, diff);
            var matchedBeforeIds = new HashSet<string>(StringComparer.Ordinal);

            if (dataBlock != null)
            {
                foreach (var row in dataBlock.Elements())
                {
                    AppendCurrentRow(result, diff, row, beforeById, matchedBeforeIds);
                }
            }

            AppendUnmatchedDeletes(result, beforeById, matchedBeforeIds);
            return result;
        }

        /// <summary>
        /// Indexes the before-image rows by their <c>diffgr:id</c> so modified rows can be paired with
        /// their originals and any unpaired before-row can be recognised as a delete.
        /// </summary>
        private static Dictionary<string, XElement> IndexBeforeRows(XElement? beforeBlock, XNamespace diff)
        {
            var beforeById = new Dictionary<string, XElement>(StringComparer.Ordinal);
            if (beforeBlock != null)
            {
                foreach (var row in beforeBlock.Elements())
                {
                    var id = row.Attribute(diff + "id")?.Value;
                    if (id != null) { beforeById[id] = row; }
                }
            }
            return beforeById;
        }

        /// <summary>
        /// Emits the before-image of every before-row that has no matching current row — those rows are
        /// deletes.
        /// </summary>
        private static void AppendUnmatchedDeletes(List<RecordFieldChange> result,
            Dictionary<string, XElement> beforeById, HashSet<string> matchedBeforeIds)
        {
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
    }
}
