namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the EnterCompany request.
    /// </summary>
    public interface IEnterCompanyRequest
    {
        /// <summary>
        /// Gets the id of the company the caller wants to enter for this session.
        /// </summary>
        string CompanyId { get; }
    }
}
