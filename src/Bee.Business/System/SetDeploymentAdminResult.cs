using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for granting or revoking a user's deployment administrator flag.
    /// </summary>
    public class SetDeploymentAdminResult : BusinessResult, ISetDeploymentAdminResponse
    {
        /// <summary>
        /// Gets or sets the user business id whose flag was set.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the flag value now stored for the user.
        /// </summary>
        public bool IsDeploymentAdmin { get; set; }
    }
}
