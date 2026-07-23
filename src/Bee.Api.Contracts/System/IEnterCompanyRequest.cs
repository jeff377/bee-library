namespace Bee.Api.Contracts.System
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
