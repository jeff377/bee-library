namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the set deployment administrator request.
    /// </summary>
    public interface ISetDeploymentAdminRequest
    {
        /// <summary>
        /// Gets the user business id (<c>st_user.sys_id</c>) whose flag is being set.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Gets a value indicating whether the user becomes a deployment administrator.
        /// </summary>
        bool IsDeploymentAdmin { get; }
    }
}
