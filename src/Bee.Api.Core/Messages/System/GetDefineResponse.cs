using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get definition operation.
    /// </summary>
    public class GetDefineResponse : ApiResponse, IGetDefineResponse
    {
        /// <summary>
        /// Gets or sets the definition XML content.
        /// </summary>
        public string Xml { get; set; } = string.Empty;
    }
}
