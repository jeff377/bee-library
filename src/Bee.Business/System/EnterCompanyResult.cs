using Bee.Api.Contracts;
using Bee.Definition.Identity;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for the EnterCompany operation.
    /// </summary>
    public class EnterCompanyResult : BusinessResult, IEnterCompanyResponse
    {
        /// <summary>
        /// Gets or sets the company information that was bound to the session.
        /// </summary>
        public CompanyInfo Company { get; set; } = new CompanyInfo();
    }
}
