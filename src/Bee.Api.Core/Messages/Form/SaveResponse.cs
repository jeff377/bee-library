using System.Data;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form Save operation.
    /// </summary>
    [MessagePackObject]
    public class SaveResponse : ApiResponse, ISaveResponse
    {
        /// <summary>
        /// Gets or sets the freshly re-loaded <c>DataSet</c>; merges in
        /// server-generated columns and resets all surviving rows to
        /// <c>RowState == Unchanged</c>.
        /// </summary>
        [Key(100)]
        public DataSet? DataSet { get; set; }

        /// <summary>
        /// Gets or sets the per-table affected-row counts (table name → rows
        /// touched).
        /// </summary>
        [Key(101)]
        public Dictionary<string, int> AffectedRows { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
