namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Holds one <see cref="ReplayWindow"/> per session.
    /// </summary>
    /// <remarks>
    /// An interface rather than a concrete type because the default implementation is per-process:
    /// with several nodes behind a load balancer and no token affinity, each node keeps its own
    /// window, so a captured packet can be replayed once per node. That is a far smaller exposure
    /// than an unbounded replay, and a deployment that cannot accept it can supply a shared store
    /// here instead.
    /// </remarks>
    public interface IReplayWindowStore
    {
        /// <summary>
        /// Returns the window for the given session, creating it on first use.
        /// </summary>
        /// <param name="accessToken">The session's access token.</param>
        /// <returns>The window to record the request's sequence in.</returns>
        ReplayWindow GetOrAdd(Guid accessToken);
    }
}
