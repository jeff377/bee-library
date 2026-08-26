using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Organization;
using Bee.Definition.Security;

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
        /// Rebuilds the session for the specified access token from its persisted seed, or returns
        /// <c>null</c> when it cannot be rebuilt.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <remarks>
        /// <c>null</c> covers every "this token is not a live session" case — no seed, an expired
        /// seed, a company permission revoked since sign-in, or a key provider that cannot recover
        /// the session key. The caller treats them all the same way: the token does not
        /// authenticate and the user signs in again.
        /// </remarks>
        SessionInfo? GetSessionInfo(Guid accessToken);

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

        /// <summary>
        /// Gets the per-form audit-rule snapshot for the specified company; <c>null</c> when the
        /// company does not exist.
        /// </summary>
        /// <param name="companyId">The company business id.</param>
        /// <remarks>
        /// A company with no rules yields an empty snapshot, not <c>null</c>: "every form inherits
        /// the deployment defaults" is an answer, whereas <c>null</c> means the company itself is
        /// unknown.
        /// <para>
        /// Defaulted rather than abstract so that adding it stayed binary-compatible for hosts with
        /// their own implementation. The default is also the correct behaviour for them: no rules
        /// means every form inherits the deployment-wide switches, which is what such a host had
        /// before per-form rules existed.
        /// </para>
        /// </remarks>
        CompanyAuditRules? GetCompanyAuditRules(string companyId) => null;

        /// <summary>
        /// Gets the enabled API key with the specified identifier; <c>null</c> when no enabled,
        /// unexpired key matches.
        /// </summary>
        /// <param name="sysId">The key identifier (the leading segment of the plaintext key).</param>
        /// <remarks>
        /// <c>null</c> covers "unknown id" and "disabled key" alike, which is what the caller needs:
        /// both reject the request, and telling them apart in the response would let a caller probe
        /// which keys exist.
        /// </remarks>
        ApiKeyInfo? GetApiKey(string sysId);

        /// <summary>
        /// Gets whether the API key gate is in force (<c>st_api_key</c> exists and holds at least
        /// one enabled key).
        /// </summary>
        /// <remarks>
        /// WARNING: implementations must let database failures propagate rather than reporting
        /// "not in force". Returning a not-in-force state on error would reopen the gate during an
        /// outage; the caller turns a propagated exception into a rejection instead. Absence of the
        /// table is a definitive schema answer and does report not-in-force.
        /// </remarks>
        ApiKeyGateState GetApiKeyGateState();
    }
}
