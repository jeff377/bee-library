using Bee.Api.Contracts;

namespace Bee.Business.Form
{
    /// <summary>
    /// Input arguments for the FormSchema-driven <c>GetData</c> operation.
    /// </summary>
    public class GetDataArgs : BusinessArgs, IGetDataRequest
    {
        /// <summary>
        /// Gets or sets the master row identifier (<c>sys_rowid</c>) to load.
        /// </summary>
        public Guid RowId { get; set; }
    }
}
