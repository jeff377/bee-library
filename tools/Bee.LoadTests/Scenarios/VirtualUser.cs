using Bee.Api.Client;
using Bee.Api.Client.Connectors;

namespace Bee.LoadTests.Scenarios
{
    /// <summary>
    /// One signed-in virtual user: its token, its own session state, and how to reach the server.
    /// </summary>
    public sealed class VirtualUser
    {
        private readonly string? _endpoint;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="accessToken">The token obtained at sign-in.</param>
        /// <param name="session">This user's own session state.</param>
        /// <param name="endpoint">The remote endpoint, or null for in-process dispatch.</param>
        public VirtualUser(Guid accessToken, ApiSessionContext session, string? endpoint)
        {
            AccessToken = accessToken;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _endpoint = endpoint;
        }

        /// <summary>Gets the access token.</summary>
        public Guid AccessToken { get; }

        /// <summary>Gets this user's session state.</summary>
        public ApiSessionContext Session { get; }

        /// <summary>
        /// Creates a form connector for a program, bound to this user's token and session.
        /// </summary>
        /// <param name="progId">The program id.</param>
        /// <returns>A connector.</returns>
        /// <remarks>
        /// A connector is created per call rather than cached: it is a thin object over the token
        /// and session, and caching one per user per program would add bookkeeping that measures
        /// nothing.
        /// </remarks>
        public FormApiConnector CreateFormConnector(string progId)
            => _endpoint is null
                ? new FormApiConnector(AccessToken, progId, Session)
                : new FormApiConnector(_endpoint, AccessToken, progId, Session);
    }
}
