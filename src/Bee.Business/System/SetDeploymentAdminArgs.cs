using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for granting or revoking a user's deployment administrator flag.
    /// </summary>
    public class SetDeploymentAdminArgs : BusinessArgs, ISetDeploymentAdminRequest
    {
        /// <summary>
        /// Gets or sets the user business id (<c>st_user.sys_id</c>) whose flag is being set.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the user becomes a deployment administrator.
        /// </summary>
        public bool IsDeploymentAdmin { get; set; }
    }
}
