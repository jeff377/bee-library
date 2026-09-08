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

        /// <summary>
        /// Signs in every virtual user the run will use, before any scenario executes.
        /// </summary>
        /// <param name="virtualUsers">The number of virtual users the run drives.</param>
        /// <returns>A task that completes once all of them hold a session.</returns>
        /// <exception cref="InvalidOperationException">
        /// A virtual user could not sign in. The run has measured nothing at this point, so the
        /// caller is expected to stop rather than continue.
        /// </exception>
        /// <remarks>
        /// IMPORTANT: signing in is a precondition, not part of the workload, and this method
        /// exists so that a failed one is reported as such. Left to happen lazily on first use it
        /// surfaced as a scenario failure instead, and doubly wrong: the faulted task stayed in
        /// the cache, so every later iteration rethrew it instantly, and because those iterations
        /// cost nothing the count ran away — one unusable account was reported as tens of millions
        /// of <c>GetList</c> failures. Neither the attribution nor the magnitude pointed at the
        /// sign-in that actually broke.
        /// </remarks>
        public async Task SignInAllAsync(int virtualUsers)
        {
            // Sequential on purpose: this is preparation rather than load, and it lets the failure
            // name the exact virtual user and account instead of whichever task faulted first.
            for (int index = 0; index < virtualUsers; index++)
            {
                try
                {
                    await GetAsync(index).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Any sign-in failure means the same thing to the operator — the
                              // run cannot start — and the type alone would not say which account
                              // it was. It is rethrown with that context, not swallowed.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    throw new InvalidOperationException(
                        $"Virtual user {index.ToString(CultureInfo.InvariantCulture)} could not " +
                        $"sign in as '{ResolveUserId(index)}', so the run stopped before " +
                        "measuring anything. Signing in is a precondition of every scenario that " +
                        "reads or writes data, not part of what a run measures. Check that " +
                        "'prepare' has been run against this database and that auth.userIdPrefix, " +
                        "auth.password and auth.companyId match the accounts it seeded. " +
                        $"({ex.GetType().Name}: {ex.Message})", ex);
                }
            }
        }

        /// <summary>
        /// Maps a virtual user index onto the account it signs in as.
        /// </summary>
        /// <param name="virtualUserIndex">The virtual user index.</param>
        /// <returns>The account id.</returns>
        private string ResolveUserId(int virtualUserIndex)
        {
            var index = _auth.TokenStrategy == TokenStrategy.Shared
                ? 0
                : virtualUserIndex % Math.Max(_auth.UserPoolSize, 1);
            return _auth.UserIdPrefix + index.ToString(CultureInfo.InvariantCulture);
        }

        private async Task<VirtualUser> SignInAsync(int virtualUserIndex)
        {
            var userId = ResolveUserId(virtualUserIndex);

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
