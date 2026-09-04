using System.Data;
using Bee.Api.Contracts.Form;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API response for the form Save operation.
    /// </summary>
    public class SaveResponse : ApiResponse, ISaveResponse
    {
        /// <summary>
        /// Gets or sets the freshly re-loaded <c>DataSet</c>; merges in
        /// server-generated columns and resets all surviving rows to
        /// <c>RowState == Unchanged</c>.
        /// </summary>
        public DataSet? DataSet { get; set; }

        /// <summary>
        /// Gets or sets the per-table affected-row counts (table name → rows
        /// touched).
        /// </summary>
        public Dictionary<string, int> AffectedRows { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        IReadOnlyDictionary<string, int> ISaveResponse.AffectedRows => AffectedRows;
    }
}
