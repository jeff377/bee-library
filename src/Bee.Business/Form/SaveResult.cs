using System.Data;
using Bee.Api.Contracts;

namespace Bee.Business.Form
{
    /// <summary>
    /// Output result for the FormSchema-driven <c>Save</c> operation.
    /// </summary>
    public class SaveResult : BusinessResult, ISaveResponse
    {
        /// <summary>
        /// Gets or sets the freshly re-loaded <c>DataSet</c> after Save.
        /// </summary>
        public DataSet? DataSet { get; set; }

        /// <summary>
        /// Gets or sets the per-table affected-row counts (table name → rows
        /// touched).
        /// </summary>
        public Dictionary<string, int> AffectedRows { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
