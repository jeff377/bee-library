using Bee.Definition.Identity;

namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Data access interface for company master records (<c>st_company</c>).
    /// </summary>
    /// <remarks>
    /// Used by <c>ICompanyInfoService</c> to fall back to the database when the
    /// in-memory cache misses. The returned <see cref="CompanyInfo"/> is the same
    /// shape that <c>EnterCompany</c> hands back to clients.
    /// </remarks>
    public interface ICompanyRepository
    {
        /// <summary>
        /// Gets the company by its business id (<c>st_company.sys_id</c>); returns
        /// <c>null</c> when no row matches. Disabled companies are still returned —
        /// callers (e.g. permission checks) decide whether the <c>enabled</c> flag
        /// matters.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        CompanyInfo? GetById(string companyId);
    }
}
