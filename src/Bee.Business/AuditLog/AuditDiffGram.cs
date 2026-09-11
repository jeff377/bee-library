using System.Data;
using System.Globalization;
using System.Text;
using System.Xml;
using Bee.Base;
using Bee.Definition;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Builds the <c>st_log_change.changes_xml</c> payload — an inline XSD followed by a DataSet
    /// DiffGram carrying both the current and the original values, which is what
    /// <see cref="ChangeDiffGramReader"/> reads back.
    /// </summary>
    /// <remarks>
    /// The form path already holds a real <c>DataSet</c> and only needs
    /// <see cref="Serialize(DataSet)"/>. The system path has no DataSet at all — a deployment-level
    /// operation writes one column of one row through a repository — so it synthesises a minimal one
    /// here rather than inventing a second payload shape. Keeping a single shape is the point: the
    /// reader, the change-detail API, and anything a deployment builds on top of them stay unaware
    /// of which path produced the row.
    /// </remarks>
    internal static class AuditDiffGram
    {
        /// <summary>
        /// The payload's outermost element. It wraps the inline XSD and the DiffGram, which XML
        /// permits only under a single root, and doubles as the discriminator
        /// <see cref="ChangeDiffGramReader"/> dispatches on — the four payload shapes the reader
        /// accepts all carry a different root name.
        /// </summary>
        public const string RootElementName = "AuditChanges";

        /// <summary>
        /// Serialises a changed DataSet to an inline XSD followed by a DiffGram. The DiffGram carries
        /// both the current and the original (before) values — plain <c>WriteXml</c> would only write
        /// current values — and the schema is what lets the reader rebuild a real
        /// <see cref="DataSet"/> from the payload alone.
        /// </summary>
        /// <param name="changes">The change set, as returned by <c>DataSet.GetChanges()</c>.</param>
        /// <remarks>
        /// <para>
        /// IMPORTANT: the schema must be written through the same <see cref="XmlWriter"/> as the
        /// DiffGram rather than by serialising the DataSet whole. <c>XmlSerializer</c> produces a
        /// byte-equivalent payload — <see cref="DataSet"/> implements <c>IXmlSerializable</c> and its
        /// <c>WriteXml</c> is these same two calls — but reaching it through <c>XmlSerializer</c>
        /// would put the audit path back on the reflection route that ADR-025 covers, for no gain.
        /// </para>
        /// <para>
        /// IMPORTANT: a value is whatever was typed or pasted into a field, and callers serialise
        /// after the change has been committed, so no string may make this method throw. Three
        /// settings keep the values intact. With <see cref="XmlWriterSettings.CheckCharacters"/> off,
        /// characters XML 1.0 forbids (the C0 controls other than tab, LF and CR, and U+FFFE / U+FFFF)
        /// are written as character references such as <c>&amp;#x1;</c> instead of throwing, and
        /// <see cref="ChangeDiffGramReader"/> turns the same check off to read them back.
        /// <see cref="NewLineHandling.Entitize"/> writes CR as <c>&amp;#xD;</c>, which XML end-of-line
        /// handling would otherwise fold into LF when the payload is read. An unpaired surrogate has
        /// no XML form at all, so <see cref="LoneSurrogateReplacingXmlWriter"/> turns it into U+FFFD.
        /// <c>AuditDiffGramCharacterTests</c> pins each of these.
        /// </para>
        /// </remarks>
        public static string Serialize(DataSet changes)
        {
            var builder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                // Indented on purpose: audit payloads are read by hand when investigating a change,
                // and the size this costs is not a constraint for the log database.
                Indent = true,
                CheckCharacters = false,
                NewLineHandling = NewLineHandling.Entitize,
            };
            using (var writer = new LoneSurrogateReplacingXmlWriter(XmlWriter.Create(builder, settings)))
            {
                writer.WriteStartElement(RootElementName);
                changes.WriteXmlSchema(writer);
                changes.WriteXml(writer, XmlWriteMode.DiffGram);
                writer.WriteEndElement();
            }
            return builder.ToString();
        }

        /// <summary>
        /// Builds an update DiffGram for a single column of a single row, so the audit record carries
        /// the before and after values rather than only the fact that something changed.
        /// </summary>
        /// <param name="tableName">The table the row belongs to.</param>
        /// <param name="rowKey">The row's <c>sys_rowid</c>.</param>
        /// <param name="fieldName">The changed column.</param>
        /// <param name="before">The value before the change.</param>
        /// <param name="after">The value after the change.</param>
        /// <param name="context">
        /// Unchanged columns identifying the affected row in human terms (a business id, a name).
        /// They are stored in the payload but do not appear in the restored field list, which by
        /// design reports only what differs.
        /// </param>
        public static string ForFieldUpdate(string tableName, string rowKey, string fieldName,
            object? before, object? after, IReadOnlyList<(string Name, object? Value)>? context = null)
        {
            var table = NewTable(tableName);
            table.Columns.Add(fieldName, typeof(string));

            var row = table.NewRow();
            row[SysFields.RowId] = rowKey;
            row[fieldName] = AsText(before);
            if (context != null)
            {
                foreach (var column in context)
                {
                    table.Columns.Add(column.Name, typeof(string));
                    row[column.Name] = AsText(column.Value);
                }
            }
            table.Rows.Add(row);

            using var dataSet = NewDataSet(table);
            // Accepting first turns the seeded values into the before-image; the assignment after it
            // is what the DiffGram records as the change.
            dataSet.AcceptChanges();
            row[fieldName] = AsText(after);

            return SerializeChanges(dataSet);
        }

        /// <summary>
        /// Builds an insert DiffGram from the supplied columns of a newly created row.
        /// </summary>
        /// <param name="tableName">The table the row was inserted into.</param>
        /// <param name="columns">The columns to record, in display order.</param>
        /// <remarks>
        /// WARNING: the caller chooses what goes in. Credentials and their hashes must never be
        /// passed — an audit row is readable by anyone who can query the log database, which is a
        /// different (and usually wider) audience than the one that may read the source table.
        /// </remarks>
        public static string ForInsert(string tableName, IReadOnlyList<(string Name, object? Value)> columns)
        {
            var table = NewTable(tableName);
            foreach (var column in columns)
            {
                table.Columns.Add(column.Name, typeof(string));
            }

            var row = table.NewRow();
            foreach (var column in columns)
            {
                row[column.Name] = AsText(column.Value);
            }
            table.Rows.Add(row);

            using var dataSet = NewDataSet(table);
            return SerializeChanges(dataSet);
        }

        /// <summary>
        /// Renders a value as the payload text. Null becomes empty rather than an absent element, so
        /// a column that was cleared still reads as a change rather than as a column that was never
        /// there.
        /// </summary>
        private static string AsText(object? value)
            => value == null ? string.Empty : ValueUtilities.CStr(value);

        /// <summary>
        /// Creates the carrier table with the row-key column the reader pairs rows on.
        /// </summary>
        private static DataTable NewTable(string tableName)
        {
            // Invariant locale: the payload is machine-read, and a server whose culture happens to
            // change must not alter how a stored value is rendered.
            var table = new DataTable(tableName) { Locale = CultureInfo.InvariantCulture };
            table.Columns.Add(SysFields.RowId, typeof(string));
            return table;
        }

        private static DataSet NewDataSet(DataTable table)
        {
            var dataSet = new DataSet("AuditChanges") { Locale = CultureInfo.InvariantCulture };
            dataSet.Tables.Add(table);
            return dataSet;
        }

        private static string SerializeChanges(DataSet dataSet)
        {
            using var changes = dataSet.GetChanges();
            return changes == null ? string.Empty : Serialize(changes);
        }
    }
}
