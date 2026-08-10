using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get common configuration operation.
    /// </summary>
    public class GetCommonConfigurationResponse : ApiResponse, IGetCommonConfigurationResponse
    {
        /// <summary>
        /// Gets or sets the common configuration content.
        /// </summary>
        public string CommonConfiguration { get; set; } = string.Empty;
    }
}
