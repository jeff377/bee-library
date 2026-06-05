using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.Services
{
    /// <summary>
    /// Layer-1 authorization service. Resolves the session's roles and the company's
    /// role-permission snapshot (both from cache — zero DB on the check path), then OR-merges the
    /// allowed action mask across the user's roles and tests the requested action.
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ISessionInfoService _sessionInfoService;
        private readonly IRolePermissionService _rolePermissionService;

        /// <summary>
        /// Initializes a new <see cref="AuthorizationService"/>.
        /// </summary>
        /// <param name="sessionInfoService">Provides the session (user id, company id, roles).</param>
        /// <param name="rolePermissionService">Provides the company's role-permission snapshot.</param>
        public AuthorizationService(ISessionInfoService sessionInfoService, IRolePermissionService rolePermissionService)
        {
            _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
            _rolePermissionService = rolePermissionService ?? throw new ArgumentNullException(nameof(rolePermissionService));
        }

        /// <inheritdoc/>
        public bool Can(Guid accessToken, string modelId, PermissionActions action)
        {
            var session = _sessionInfoService.Get(accessToken);
            if (session == null || string.IsNullOrEmpty(session.CompanyId) || session.Roles.Count == 0)
            {
                return false;
            }

            var snapshot = _rolePermissionService.Get(session.CompanyId);
            if (snapshot == null) { return false; }

            return snapshot.GetAllowed(session.Roles, modelId).HasFlag(action);
        }
    }
}
