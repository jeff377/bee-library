namespace Bee.Definition.Identity
{
    /// <summary>
    /// Decides whether a session may act on a deployment-level asset.
    /// </summary>
    /// <remarks>
    /// WARNING: this is <b>not</b> a second entry point to <see cref="ICompanyAuthorizationService"/> and
    /// the two must never fall back to one another. Company authorization answers "may this user do
    /// X inside company C" and returns <c>false</c> without a company context; this one answers "may
    /// this user do X to the installation itself", where no company context exists by definition.
    /// A deployment administrator gains no data rights in any company, and a company administrator
    /// gains nothing here.
    /// </remarks>
    public interface IDeploymentAuthorizationService
    {
        /// <summary>
        /// Determines whether the session behind <paramref name="accessToken"/> may perform
        /// <paramref name="action"/>.
        /// </summary>
        /// <param name="accessToken">The caller's access token.</param>
        /// <param name="action">The deployment-level operation being attempted.</param>
        /// <returns><c>true</c> when the caller is authorized; otherwise, <c>false</c>.</returns>
        bool Can(Guid accessToken, DeploymentAction action);
    }
}
