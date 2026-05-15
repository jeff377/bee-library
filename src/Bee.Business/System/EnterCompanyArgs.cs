using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for the EnterCompany operation.
    /// </summary>
    public class EnterCompanyArgs : BusinessArgs, IEnterCompanyRequest
    {
        /// <summary>
        /// Gets or sets the id of the company the caller wants to enter for this session.
        /// </summary>
        public string CompanyId { get; set; } = string.Empty;
    }
}
