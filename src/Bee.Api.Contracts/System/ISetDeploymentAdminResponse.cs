namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the set deployment administrator response.
    /// </summary>
    public interface ISetDeploymentAdminResponse
    {
        /// <summary>
        /// Gets the user business id whose flag was set.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Gets the flag value now stored for the user.
        /// </summary>
        bool IsDeploymentAdmin { get; }
    }
}
