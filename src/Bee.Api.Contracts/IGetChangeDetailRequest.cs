namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get-change-detail request: identifies one change event by its log
    /// row id, whose <c>changes_xml</c> DiffGram is restored into structured before/after values.
    /// </summary>
    public interface IGetChangeDetailRequest
    {
        /// <summary>
        /// Gets the change event's log row id (<c>st_log_change.sys_rowid</c>), as returned in a
        /// change-log or record-history list row.
        /// </summary>
        Guid SysRowId { get; }
    }
}
