using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the set deployment administrator operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class SetDeploymentAdminResponse : ApiResponse, ISetDeploymentAdminResponse
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
