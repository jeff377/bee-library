using System.Globalization;
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Running;

namespace Bee.LoadTests.Scenarios
{
    /// <summary>
    /// Signs an account in, measuring the only path that creates a session.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: every call builds its own <see cref="ApiSessionContext"/> rather than using
    /// <see cref="ApiSessionContext.Ambient"/>. The ambient instance is a single process-wide
    /// object, so concurrent sign-ins through it overwrite one another's transmission key; the
    /// symptom is not a clear error but decryption failures elsewhere that read like framework
    /// instability under load.
    /// </remarks>
    public sealed class LoginScenario : IScenario
    {
        private readonly AuthOptions _auth;
        private readonly string? _endpoint;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="auth">Authentication configuration.</param>
        /// <param name="endpoint">
        /// The remote endpoint, or null to dispatch in-process.
        /// </param>
        public LoginScenario(AuthOptions auth, string? endpoint = null)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _endpoint = string.IsNullOrWhiteSpace(endpoint) ? null : endpoint;
        }

        /// <inheritdoc/>
        public string Name => "Login";

        /// <inheritdoc/>
        public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
        {
            var userId = ResolveUserId(context.VirtualUserIndex);
            var session = new ApiSessionContext();

            var connector = _endpoint is null
                ? new SystemApiConnector(Guid.Empty, session)
                : new SystemApiConnector(_endpoint, Guid.Empty, session);

            await connector.LoginAsync(userId, _auth.Password).ConfigureAwait(false);
        }

        /// <summary>
        /// Maps a virtual user to the account it signs in as.
        /// </summary>
        /// <param name="virtualUserIndex">The virtual user index.</param>
        /// <returns>The account id.</returns>
        /// <remarks>
        /// Under <see cref="TokenStrategy.Shared"/> every user signs in as the first account,
        /// which measures one account being hammered. Under
        /// <see cref="TokenStrategy.PerUser"/> the index wraps around the seeded pool, so a run
        /// with more virtual users than accounts still works — it just shares accounts.
        /// </remarks>
        internal string ResolveUserId(int virtualUserIndex)
        {
            var index = _auth.TokenStrategy == TokenStrategy.Shared
                ? 0
                : virtualUserIndex % Math.Max(_auth.UserPoolSize, 1);
            return _auth.UserIdPrefix + index.ToString(CultureInfo.InvariantCulture);
        }
    }
}
