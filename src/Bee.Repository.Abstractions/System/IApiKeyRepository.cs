using Bee.Definition.Security;

namespace Bee.Repository.Abstractions.System
{
    /// <summary>
    /// Data access interface for issued API keys (<c>st_api_key</c> in the common database).
    /// </summary>
    /// <remarks>
    /// Backs <c>ApiKeyCache</c> through <c>ICacheDataSourceProvider</c>, so reads here run on a
    /// cache miss rather than on every request.
    /// </remarks>
    public interface IApiKeyRepository
    {
        /// <summary>
        /// Gets the enabled key with the specified identifier; <c>null</c> when the identifier is
        /// unknown or the key is disabled.
        /// </summary>
        /// <param name="sysId">The key identifier (<c>sys_id</c>).</param>
        /// <remarks>
        /// Disabled rows are filtered by the query, so to callers they are indistinguishable from
        /// keys that never existed — matching how <see cref="ICompanyRepository"/> treats disabled
        /// companies, and matching the single merged rejection the API surfaces.
        /// <para>
        /// Expired rows ARE returned: expiry is evaluated against the current time on every
        /// validation, which is what makes an expiry take effect immediately rather than when the
        /// cache entry happens to lapse.
        /// </para>
        /// </remarks>
        ApiKeyInfo? GetEnabledById(string sysId);

        /// <summary>
        /// Gets whether the key gate is in force: the table exists and holds at least one enabled
        /// key.
        /// </summary>
        /// <remarks>
        /// WARNING: only a definitive answer may report not-in-force — the table is absent, or it
        /// holds no enabled row. Database failures must propagate so the caller can fail closed;
        /// swallowing them here would reopen the gate during an outage.
        /// <para>
        /// NOTE: expiry is deliberately not considered. A deployment whose only key has expired
        /// keeps the gate in force, because silently reverting to "any non-empty key passes" would
        /// turn a lapsed key into an open API.
        /// </para>
        /// </remarks>
        ApiKeyGateState GetGateState();

        /// <summary>
        /// Returns whether a key row with the specified identifier exists, enabled or not.
        /// </summary>
        /// <param name="sysId">The key identifier (<c>sys_id</c>).</param>
        /// <remarks>
        /// Lets the issuing path reject a duplicate identifier with a usable message instead of
        /// surfacing a unique-index violation from the provider.
        /// </remarks>
        bool Exists(string sysId);

        /// <summary>
        /// Inserts a new enabled key row.
        /// </summary>
        /// <param name="apiKey">
        /// The key metadata, with <see cref="ApiKeyInfo.HashedKey"/> already hashed. The plaintext
        /// secret is never passed here, and the framework keeps no copy of it.
        /// </param>
        void Insert(ApiKeyInfo apiKey);

        /// <summary>
        /// Lists every issued key, enabled or not, ordered by identifier.
        /// </summary>
        /// <returns>The keys as operator-facing summaries.</returns>
        /// <remarks>
        /// WARNING: returns <see cref="ApiKeySummary"/>, never <see cref="ApiKeyInfo"/> — the latter
        /// carries the credential hash and must not leave the server.
        /// <para>
        /// Unlike <see cref="GetEnabledById"/>, disabled rows are included: hiding them here would
        /// make a disabled key indistinguishable from a free identifier, and re-issuing that
        /// identifier is exactly what must not silently happen.
        /// </para>
        /// </remarks>
        IReadOnlyList<ApiKeySummary> GetList();

        /// <summary>
        /// Enables or disables a key, returning <c>false</c> when the identifier is unknown.
        /// </summary>
        /// <param name="sysId">The key identifier (<c>sys_id</c>).</param>
        /// <param name="enabled">The value to store.</param>
        /// <remarks>
        /// WARNING: the write and its cache invalidation must share one transaction, as in
        /// <see cref="Insert"/>. Disabling is a revocation — an announcement that is missed leaves
        /// the key working until the cached entry lapses, which is the one outcome revocation may
        /// not have.
        /// </remarks>
        bool SetEnabled(string sysId, bool enabled);

        /// <summary>
        /// Sets or clears a key's expiry, returning <c>false</c> when the identifier is unknown.
        /// </summary>
        /// <param name="sysId">The key identifier (<c>sys_id</c>).</param>
        /// <param name="expiredAt">The UTC expiry, or <c>null</c> for a key that does not expire.</param>
        /// <remarks>
        /// Expiry is evaluated on every validation rather than through cache expiry, but the cached
        /// entry still holds the old value, so this invalidates on the same terms as
        /// <see cref="SetEnabled"/>.
        /// </remarks>
        bool SetExpiry(string sysId, DateTime? expiredAt);
    }
}
