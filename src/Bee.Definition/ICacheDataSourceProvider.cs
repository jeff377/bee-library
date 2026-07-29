using Bee.Definition.Identity;
using Bee.Definition.Organization;

namespace Bee.Definition
{
    /// <summary>
    /// Supplies the database-backed caches with their data. Each method is the sole load path for
    /// one cache: the cache calls it on a miss (from its <c>CreateInstance</c> override) and caches
    /// whatever comes back.
    /// </summary>
    /// <remarks>
    /// Every method returns a definition-layer type rather than a repository. That is deliberate:
    /// this interface lives in <c>Bee.Definition</c>, which <c>Bee.Repository.Abstractions</c>
    /// depends on, so exposing a repository type here would close a circular project reference.
    /// <para>
    /// WARNING: implementations must be resolved lazily, on the first cache miss. Resolving one
    /// while the cache container is still under construction closes the dependency cycle
    /// <c>ICacheContainer</c> to <c>ICacheDataSourceProvider</c> to the repository factory to
    /// <c>IDefineAccess</c> and back to <c>ICacheContainer</c>.
    /// </para>
    /// </remarks>
    public interface ICacheDataSourceProvider
    {
        /// <summary>
        /// Gets the session user data for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        SessionUser? GetSessionUser(Guid accessToken);

        /// <summary>
        /// Gets the company master record for the specified company id; <c>null</c> when no company
        /// matches.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        CompanyInfo? GetCompanyInfo(string companyId);

        /// <summary>
        /// Gets the role-permission snapshot for the specified company; <c>null</c> when the company
        /// does not exist.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        CompanyRolePermissions? GetCompanyRolePermissions(string companyId);

        /// <summary>
        /// Gets the department tree snapshot for the specified company; <c>null</c> when the company
        /// does not exist.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        DepartmentTree? GetDepartmentTree(string companyId);
    }
}
