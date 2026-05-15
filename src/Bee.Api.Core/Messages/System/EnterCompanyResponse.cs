using Bee.Api.Contracts;
using Bee.Definition.Identity;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the EnterCompany operation.
    /// </summary>
    [MessagePackObject]
    public class EnterCompanyResponse : ApiResponse, IEnterCompanyResponse
    {
        /// <summary>
        /// Gets or sets the company information that was bound to the session.
        /// </summary>
        [Key(100)]
        public CompanyInfo Company { get; set; } = new CompanyInfo();
    }
}
