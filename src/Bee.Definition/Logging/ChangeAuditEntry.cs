namespace Bee.Definition.Logging
{
    /// <summary>
    /// The kind of data change recorded in <c>st_log_change</c>, derived from the master row's
    /// state. A details-only change (master unchanged) still counts as an <see cref="Update"/>.
    /// </summary>
    public enum ChangeKind
    {
        /// <summary>A new record was inserted.</summary>
        Insert = 1,

        /// <summary>An existing record was updated (including details-only changes).</summary>
        Update = 2,

        /// <summary>A record was deleted.</summary>
        Delete = 3,
    }

    /// <summary>
    /// Audit entry for a data-change event (row in <c>st_log_change</c>). One entry captures one
    /// <c>Save</c> / <c>Delete</c> as a whole: the master and all its details, every changed row and
    /// field, and their before/after values, serialised into <see cref="ChangesXml"/> as a DataSet
    /// DiffGram. The header columns stay queryable without parsing the XML.
    /// </summary>
    public sealed class ChangeAuditEntry : AuditEntry
    {
        /// <inheritdoc/>
        public override string TableName => "st_log_change";

        /// <summary>Gets the business object (program) id.</summary>
        public string ProgId { get; init; } = string.Empty;

        /// <summary>Gets the master table name.</summary>
        public string ChangeTableName { get; init; } = string.Empty;

        /// <summary>Gets the master record key (its <c>sys_rowid</c>), for querying a record's history.</summary>
        public string? RowKey { get; init; }

        /// <summary>Gets the change kind derived from the master row state.</summary>
        public ChangeKind ChangeKind { get; init; }

        /// <summary>Gets a value indicating whether this change touches sensitive data (axis ⑥).</summary>
        public bool IsSensitive { get; init; }

        /// <summary>
        /// Gets the change payload: a DataSet DiffGram XML carrying every changed row/field and its
        /// before/after values. A delete records the full before-image (the deleted record's values).
        /// </summary>
        public string ChangesXml { get; init; } = string.Empty;

        /// <inheritdoc/>
        protected override void AddColumns(IList<AuditColumn> columns)
        {
            columns.Add(new AuditColumn("prog_id", ProgId));
            columns.Add(new AuditColumn("table_name", ChangeTableName));
            columns.Add(new AuditColumn("row_key", RowKey));
            columns.Add(new AuditColumn("change_kind", (int)ChangeKind));
            columns.Add(new AuditColumn("is_sensitive", IsSensitive));
            columns.Add(new AuditColumn("changes_xml", ChangesXml));
        }
    }
}
