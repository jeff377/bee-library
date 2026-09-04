using Bee.Base;

namespace Bee.Definition.Security
{
    /// <summary>
    /// Whether the API key gate is in force for this deployment: <c>st_api_key</c> exists and holds
    /// at least one enabled key.
    /// </summary>
    /// <remarks>
    /// A deployment that has never issued a key keeps the pre-gate behaviour (a non-empty
    /// <c>X-Api-Key</c> is enough), so upgrading the framework does not lock existing callers out.
    /// Issuing the first key closes the gate on its own, with no code to write.
    /// <para>
    /// WARNING: only a definitive schema answer may produce <see cref="InForce"/> <c>false</c> —
    /// the table is absent, or it is present and holds no enabled key. A database failure must
    /// never be read as "no keys": that would turn an availability incident into an open gate.
    /// The lookup path therefore lets database exceptions propagate instead of returning an
    /// instance of this type, and callers fail closed. See <see cref="IApiKeyValidator"/>.
    /// </para>
    /// <para>
    /// WARNING: this is a cache-shared instance. It must not be mutated after it is loaded — the
    /// whole deployment reads the same reference, so flipping <see cref="InForce"/> on it opens or
    /// closes the gate for every caller at once. The setter exists for the serializers, not for
    /// callers. See <c>docs/development-constraints.md</c> § <i>Cached Data Immutability After
    /// Init</i>.
    /// </para>
    /// </remarks>
    public class ApiKeyGateState : IKeyObject
    {
        /// <summary>
        /// The single cache key this state is stored under. Shares the cache group of
        /// <see cref="ApiKeyInfo"/> so key changes and gate changes are invalidated by the same
        /// notify group, and is bracketed to keep it disjoint from any real <c>sys_id</c>
        /// (which <see cref="Bee.Base.Security.ApiKeyFormat"/> restricts to lowercase letters, digits and hyphens).
        /// </summary>
        public const string CacheKey = "[gate]";

        /// <summary>
        /// Gets or sets a value indicating whether at least one enabled API key exists, which is
        /// what puts the gate in force.
        /// </summary>
        public bool InForce { get; set; }

        /// <summary>
        /// Gets the cache key (always <see cref="CacheKey"/>).
        /// </summary>
        public string GetKey()
        {
            return CacheKey;
        }
    }
}
