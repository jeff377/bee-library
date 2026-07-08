using System.Data;
using Bee.Definition.Paging;

namespace Bee.Repository.Abstractions.AuditLog
{
    /// <summary>
    /// A page of audit-log rows: the header <see cref="DataTable"/> plus the <see cref="PagingInfo"/>
    /// describing the page (effective page / size, optional total count, and whether more rows exist).
    /// </summary>
    public sealed class AuditLogPage
    {
        /// <summary>Gets the page's rows.</summary>
        public DataTable Table { get; init; } = new DataTable();

        /// <summary>Gets the paging metadata for this page.</summary>
        public PagingInfo Paging { get; init; } = new PagingInfo();
    }
}
