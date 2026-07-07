namespace Bee.Definition.Logging
{
    /// <summary>
    /// Audit entry for a read/access event (row in <c>st_log_access</c>). One entry records that a
    /// user viewed one record's detail (a <c>GetData</c> call): who viewed which record. Field-level
    /// detail is intentionally not recorded — a detail view loads the whole record, so the record
    /// key plus the viewer is the unit of interest.
    /// </summary>
    public sealed class AccessAuditEntry : AuditEntry
    {
        /// <inheritdoc/>
        public override string TableName => "st_log_access";

        /// <summary>Gets the business object (program) id whose record was viewed.</summary>
        public string ProgId { get; init; } = string.Empty;

        /// <summary>Gets the viewed record's key (its <c>sys_rowid</c>).</summary>
        public string? RowKey { get; init; }

        /// <inheritdoc/>
        protected override void AddColumns(IList<AuditColumn> columns)
        {
            columns.Add(new AuditColumn("prog_id", ProgId));
            columns.Add(new AuditColumn("row_key", RowKey));
        }
    }
}
