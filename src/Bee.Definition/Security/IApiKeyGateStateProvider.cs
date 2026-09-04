namespace Bee.Definition.Security
{
    /// <summary>
    /// Answers whether the deployment's API key gate is in force.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The state itself lives in <see cref="ApiKeyGateState"/> and is served from a cache, but a
    /// caller that only wants the answer should not have to know that. This seam exists so the API
    /// layer can ask the question without taking a type dependency on the caching implementation
    /// assembly — the layering constraint that forbids the API layer from reaching into the
    /// Repository layer applies to the cache for the same reason.
    /// </para>
    /// <para>
    /// Deliberately read-only and deliberately one method. Widening it would turn it into a second
    /// door onto the cache, which is what it exists to avoid.
    /// </para>
    /// </remarks>
    public interface IApiKeyGateStateProvider
    {
        /// <summary>
        /// Returns the current gate state, or <c>null</c> when there is no key store to consult.
        /// </summary>
        /// <returns>
        /// The gate state, or <c>null</c> when no key store is configured at all — the normal shape
        /// for an in-process host, and the same answer as a store holding no keys.
        /// </returns>
        /// <remarks>
        /// WARNING: <c>null</c> and an exception mean different things and callers must keep them
        /// apart. <c>null</c> is "there is nothing to ask"; an exception is "the store exists and
        /// could not be reached", which at run time rejects every call. Reporting the second as the
        /// first would read an unreachable database as an open gate.
        /// </remarks>
        ApiKeyGateState? GetState();
    }
}
