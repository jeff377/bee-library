using Bee.Definition.Identity;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Default <see cref="IDeploymentAuthorizationService"/>: resolves the session, then reads the
    /// user's <c>st_user.deployment_admin</c> flag from the common database. The flag answers every
    /// action for now; the action parameter is carried so a later split into finer grants changes
    /// this class alone.
    /// </summary>
    /// <remarks>
    /// NOTE: unlike <see cref="IAuthorizationService"/>, which resolves both the session and the
    /// company snapshot from cache and never touches the database, this check queries on every call.
    /// That is deliberate. Deployment-level operations are rare (minting a key, appointing an
    /// administrator), so the saved query does not pay for a cache and its invalidation; and
    /// revoking an administrator has to take effect immediately, which every cached form of the flag
    /// would delay. Should a high-frequency consumer appear, revisit it here.
    /// <para>
    /// Failures are closed: an unknown token, an unknown user, or a database error all deny.
    /// </para>
    /// </remarks>
    public class DeploymentAuthorizationService : IDeploymentAuthorizationService
    {
        private readonly ISessionInfoService _sessionInfoService;
        private readonly IRepositoryFactory _repositoryFactory;

        /// <summary>
        /// Initializes a new <see cref="DeploymentAuthorizationService"/>.
        /// </summary>
        /// <param name="sessionInfoService">Provides the session behind the access token.</param>
        /// <param name="repositoryFactory">Factory that builds framework repositories on demand.</param>
        public DeploymentAuthorizationService(ISessionInfoService sessionInfoService, IRepositoryFactory repositoryFactory)
        {
            _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        }

        /// <inheritdoc/>
        public bool Can(Guid accessToken, DeploymentAction action)
        {
            var session = _sessionInfoService.Get(accessToken);
            if (session == null || string.IsNullOrEmpty(session.UserId))
            {
                return false;
            }

            // No company context is required or consulted: a deployment administrator is an
            // administrator of the installation, and company membership says nothing about it.
            return _repositoryFactory.Create<IUserRepository>().IsDeploymentAdmin(session.UserId);
        }
    }
}
