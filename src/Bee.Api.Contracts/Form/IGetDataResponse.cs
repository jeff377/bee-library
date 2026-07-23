using System.Data;

namespace Bee.Api.Contracts.Form
{
    /// <summary>
    /// Contract interface for the GetData response.
    /// </summary>
    public interface IGetDataResponse
    {
        /// <summary>
        /// Gets the loaded <c>DataSet</c>. The master table is named after the
        /// program identifier (framework invariant) and contains exactly one
        /// row; detail tables (when requested) carry their schema-declared
        /// names. All rows have been <c>AcceptChanges</c>'d so
        /// <c>RowState == Unchanged</c>. <c>null</c> when no row matches
        /// <c>RowId</c>.
        /// </summary>
        DataSet? DataSet { get; }
    }
}
