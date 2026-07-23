using System.Data;

namespace Bee.Api.Contracts.Form
{
    /// <summary>
    /// Contract interface for the GetNewData response.
    /// </summary>
    public interface IGetNewDataResponse
    {
        /// <summary>
        /// Gets the blank <c>DataSet</c> skeleton. The master table contains
        /// exactly one row in the <c>Added</c> state with FormSchema defaults
        /// applied and a server-issued <c>sys_rowid</c>; detail tables (when
        /// requested) carry their full schema but no rows.
        /// </summary>
        DataSet? DataSet { get; }
    }
}
