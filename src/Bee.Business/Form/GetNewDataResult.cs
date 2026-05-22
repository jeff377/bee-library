using System.Data;
using Bee.Api.Contracts;

namespace Bee.Business.Form
{
    /// <summary>
    /// Output result for the FormSchema-driven <c>GetNewData</c> operation.
    /// </summary>
    public class GetNewDataResult : BusinessResult, IGetNewDataResponse
    {
        /// <summary>
        /// Gets or sets the blank <c>DataSet</c> skeleton.
        /// </summary>
        public DataSet? DataSet { get; set; }
    }
}
