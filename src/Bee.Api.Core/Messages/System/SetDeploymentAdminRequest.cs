using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the set deployment administrator operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class SetDeploymentAdminRequest : ApiRequest, ISetDeploymentAdminRequest
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
