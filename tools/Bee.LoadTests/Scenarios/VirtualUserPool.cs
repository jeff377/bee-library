using System.Collections.Concurrent;
using System.Globalization;
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.LoadTests.Configuration;

namespace Bee.LoadTests.Scenarios
{
    /// <summary>
    /// Signs each virtual user in once and hands out its session to the scenarios that need one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read and write scenarios measure the call, not the sign-in that precedes it, so each virtual
    /// user authenticates once and reuses the result. <see cref="LoginScenario"/> deliberately does
    /// not go through this pool — signing in is the thing it measures.
    /// </para>
    /// <para>
    /// IMPORTANT: each user gets its own <see cref="ApiSessionContext"/>. Sharing
    /// <see cref="ApiSessionContext.Ambient"/> would let concurrent sign-ins overwrite one
    /// another's transmission key, and the resulting decryption failures read like framework
    /// instability rather than a load-test defect.
    /// </para>
    /// </remarks>
    public sealed class VirtualUserPool
    {
        private readonly ConcurrentDictionary<int, Lazy<Task<VirtualUser>>> _users = new();
        private readonly AuthOptions _auth;
        private readonly string? _endpoint;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="auth">Authentication configuration.</param>
        /// <param name="endpoint">The remote endpoint, or null to dispatch in-process.</param>
        public VirtualUserPool(AuthOptions auth, string? endpoint = null)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _endpoint = string.IsNullOrWhiteSpace(endpoint) ? null : endpoint;
        }

        /// <summary>
        /// Gets the signed-in session for a virtual user, signing it in on first use.
        /// </summary>
        /// <param name="virtualUserIndex">The virtual user index.</param>
        /// <returns>The signed-in session.</returns>
        public Task<VirtualUser> GetAsync(int virtualUserIndex)
            => _users.GetOrAdd(virtualUserIndex,
                index => new Lazy<Task<VirtualUser>>(() => SignInAsync(index))).Value;

        private async Task<VirtualUser> SignInAsync(int virtualUserIndex)
        {
            var index = _auth.TokenStrategy == TokenStrategy.Shared
                ? 0
                : virtualUserIndex % Math.Max(_auth.UserPoolSize, 1);
            var userId = _auth.UserIdPrefix + index.ToString(CultureInfo.InvariantCulture);

            var session = new ApiSessionContext();
            var system = _endpoint is null
                ? new SystemApiConnector(Guid.Empty, session)
                : new SystemApiConnector(_endpoint, Guid.Empty, session);

            var login = await system.LoginAsync(userId, _auth.Password).ConfigureAwait(false);

            // Entering a company is a separate step the framework requires before form data is
            // reachable; a session that skipped it has no company to route category "company" to.
            var entered = _endpoint is null
                ? new SystemApiConnector(login.AccessToken, session)
                : new SystemApiConnector(_endpoint, login.AccessToken, session);
            await entered.EnterCompanyAsync(_auth.CompanyId).ConfigureAwait(false);

            return new VirtualUser(login.AccessToken, session, _endpoint);
        }
    }
}
