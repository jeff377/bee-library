using System.Data;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the Save request.
    /// </summary>
    /// <remarks>
    /// Step 2 of both the new-and-save and load-and-save flows. The single
    /// <c>Save</c> action handles INSERT / UPDATE / DELETE by dispatching on
    /// each row's <c>RowState</c>:
    /// <list type="bullet">
    /// <item><c>Added</c> → INSERT</item>
    /// <item><c>Modified</c> → UPDATE</item>
    /// <item><c>Deleted</c> → DELETE</item>
    /// <item><c>Unchanged</c> / <c>Detached</c> → skipped</item>
    /// </list>
    /// </remarks>
    public interface ISaveRequest
    {
        /// <summary>
        /// Gets the <c>DataSet</c> to persist. Carries both the master row and
        /// all detail tables; the server inspects <c>RowState</c> on every row
        /// to decide the SQL to execute.
        /// </summary>
        DataSet? DataSet { get; }
    }
}
