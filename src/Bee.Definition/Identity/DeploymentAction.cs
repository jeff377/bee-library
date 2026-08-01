namespace Bee.Definition.Identity
{
    /// <summary>
    /// An operation on a deployment-level asset — one that belongs to the installation rather than
    /// to any single company.
    /// </summary>
    /// <remarks>
    /// Deliberately a separate axis from <see cref="Settings.PermissionAction"/>, which is company-scoped:
    /// company roles live in each company's own database, so granting a company administrator any
    /// say over installation-wide assets would let one tenant act for all of them.
    /// <para>
    /// The enumeration starts with the single action the framework needs today. Carrying the action
    /// in the signature from the outset is the point — a later split into finer actions changes
    /// only <see cref="IDeploymentAuthorizationService"/> implementations, not their callers.
    /// </para>
    /// </remarks>
    public enum DeploymentAction
    {
        /// <summary>
        /// Issue, revoke or inspect API keys (<c>st_api_key</c>).
        /// </summary>
        ManageApiKey = 1
    }
}
