using System.Data;
using Bee.Api.Contracts.Form;

namespace Bee.Business.Form
{
    /// <summary>
    /// Input arguments for the FormSchema-driven <c>Save</c> operation.
    /// </summary>
    public class SaveArgs : BusinessArgs, ISaveRequest
    {
        /// <summary>
        /// Gets or sets the <c>DataSet</c> to persist. Each row's
        /// <c>RowState</c> determines whether INSERT / UPDATE / DELETE is
        /// executed.
        /// </summary>
        public DataSet? DataSet { get; set; }
    }
}
