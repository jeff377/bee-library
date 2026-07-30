using Bee.Base.Security;
using Bee.Definition.Security;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Default <see cref="IApiKeyValidator"/>: identifies the calling application from the
    /// <c>X-Api-Key</c> header, reading the issued keys through the cache.
    /// </summary>
    /// <remarks>
    /// The work on a cache hit is a string split, one dictionary lookup, one SHA-256 over about
    /// 48 bytes and a fixed-time compare — the same order of cost the framework already pays to
    /// validate an access token, which is why keys are checked per request instead of being
    /// exchanged for a short-lived token.
    /// <para>
    /// Two behaviours are load-bearing and must not be "tidied":
    /// every rejection returns the same <see cref="ApiKeyStatus.Invalid"/> status regardless of
    /// reason, so the API cannot be used to discover which identifiers exist; and lookup failures
    /// are NOT caught here, so a database outage cannot be mistaken for "this deployment has no
    /// keys". The caller converts a thrown exception into a rejection.
    /// </para>
    /// </remarks>
    public class ApiKeyValidator : IApiKeyValidator
    {
        private readonly ICacheContainer _cache;

        /// <summary>
        /// Initializes a new <see cref="ApiKeyValidator"/>.
        /// </summary>
        /// <param name="cache">The cache container holding the key and gate caches.</param>
        public ApiKeyValidator(ICacheContainer cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <inheritdoc/>
        public ApiKeyValidationResult Validate(string? apiKey)
        {
            var gate = _cache.ApiKeyGate.GetState();

            // A null state means there is no key store to consult at all — the cache was built
            // without a data source, which is the normal shape for an in-process host. That is the
            // same situation as a store holding no keys, not a failure: a failure would have thrown.
            if (gate == null || !gate.InForce)
            {
                return new ApiKeyValidationResult(ApiKeyStatus.NotConfigured);
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new ApiKeyValidationResult(ApiKeyStatus.NotProvided);
            }

            // A value that cannot be split never reaches the database. Together with the negative
            // caching of unknown identifiers, this is what makes the absence of rate limiting
            // acceptable: scanning costs an attacker a string operation and a memory lookup.
            if (!ApiKeyFormat.TryParse(apiKey, out string sysId, out string secret))
            {
                return Rejected(string.Empty);
            }

            var info = _cache.ApiKey.Get(sysId);
            if (info == null)
            {
                return Rejected(sysId);
            }
            if (info.IsExpired(DateTime.UtcNow))
            {
                return Rejected(sysId);
            }
            if (!ApiKeyHasher.VerifySecret(secret, info.HashedKey))
            {
                return Rejected(sysId);
            }

            return new ApiKeyValidationResult(ApiKeyStatus.Valid, info.SysId, info.SysName);
        }

        /// <summary>
        /// Builds the rejection result shared by every failure reason (malformed, unknown, disabled,
        /// expired, wrong secret).
        /// </summary>
        /// <param name="sysId">
        /// The identifier the caller presented, or an empty string when the value did not parse. Kept
        /// so the audit record can name the attempt; it is not secret, and its character set has been
        /// validated, so it is safe to log.
        /// </param>
        private static ApiKeyValidationResult Rejected(string sysId)
        {
            return new ApiKeyValidationResult(ApiKeyStatus.Invalid, sysId, string.Empty);
        }
    }
}
