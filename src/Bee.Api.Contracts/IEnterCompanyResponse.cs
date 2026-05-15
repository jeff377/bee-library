using Bee.Definition.Identity;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the EnterCompany response.
    /// </summary>
    public interface IEnterCompanyResponse
    {
        /// <summary>
        /// Gets the company information that was bound to the session.
        /// </summary>
        CompanyInfo Company { get; }
    }
}
