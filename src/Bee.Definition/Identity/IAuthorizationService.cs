using Bee.Definition.Settings;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// Layer-1 authorization: whether the session's user may perform an action on a permission
    /// model within their current company. Pure <c>(model, action)</c> gate — record-scope
    /// filtering (layer 2) is a separate concern.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Returns <c>true</c> when the session identified by <paramref name="accessToken"/> is
        /// authorized for <paramref name="action"/> on <paramref name="modelId"/>; otherwise
        /// <c>false</c> (not logged in, no company entered, no roles, or no grant).
        /// </summary>
        /// <param name="accessToken">The session's access token.</param>
        /// <param name="modelId">The permission model id.</param>
        /// <param name="action">The action to check (a single <see cref="PermissionAction"/> flag).</param>
        bool Can(Guid accessToken, string modelId, PermissionAction action);
    }
}
