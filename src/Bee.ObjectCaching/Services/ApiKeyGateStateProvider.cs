using Bee.Definition.Security;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Serves <see cref="IApiKeyGateStateProvider"/> from the API key gate cache.
    /// </summary>
    /// <remarks>
    /// A one-method pass-through, and that is the point: the caller needs the answer, not the cache.
    /// Without it the API layer resolved <c>ICacheContainer</c> directly, which put a type dependency
    /// on this assembly — the caching implementation — into a layer that should only speak the
    /// definition vocabulary.
    /// </remarks>
    /// <param name="cache">The cache container holding the gate state.</param>
    public sealed class ApiKeyGateStateProvider(ICacheContainer cache) : IApiKeyGateStateProvider
    {
        private readonly ICacheContainer _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        /// <inheritdoc/>
        public ApiKeyGateState? GetState() => _cache.ApiKeyGate.GetState();
    }
}
