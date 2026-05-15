namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Permission lookup for the user-company access table (<c>st_user_company</c>).
    /// </summary>
    /// <remarks>
    /// Backs the permission check inside <c>EnterCompany</c>. The interface intentionally
    /// exposes only the read-side <see cref="HasAccess"/> for now; write-side helpers
    /// (Grant / Revoke) will be promoted to the interface when the Company / UserCompany
    /// admin BO lands in a follow-up plan.
    /// </remarks>
    public interface IUserCompanyRepository
    {
        /// <summary>
        /// Determines whether the user is granted access to the company. Returns <c>true</c>
        /// only when (a) the user-company row exists and (b) the target company is enabled.
        /// Returns <c>false</c> for nonexistent users, nonexistent companies, disabled
        /// companies, or missing grants — callers decide whether to merge these into a
        /// single denied-access error surface.
        /// </summary>
        /// <param name="userId">The user business id (<c>st_user.sys_id</c>).</param>
        /// <param name="companyId">The company business id (<c>st_company.sys_id</c>).</param>
        bool HasAccess(string userId, string companyId);
    }
}
