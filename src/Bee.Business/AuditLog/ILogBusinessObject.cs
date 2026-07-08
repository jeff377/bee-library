namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Cross-BO interface for the audit-log business object (read-only queries over the
    /// <c>st_log_*</c> tables). Exists so other BOs can resolve it through
    /// <c>IBusinessObjectFactory</c> and query the audit trail without binding to the concrete class.
    /// </summary>
    public interface ILogBusinessObject : IBusinessObject
    {
        /// <summary>
        /// Gets a record's change history (all <c>st_log_change</c> events for a <c>progId</c> +
        /// <c>rowKey</c>), each with its DiffGram restored into structured before/after field values.
        /// </summary>
        /// <param name="args">The input arguments carrying the target <c>ProgId</c> and <c>RowKey</c>.</param>
        GetRecordHistoryResult GetRecordHistory(GetRecordHistoryArgs args);
    }
}
