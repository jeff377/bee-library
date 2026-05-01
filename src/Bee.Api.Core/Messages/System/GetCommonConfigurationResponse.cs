using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the get common configuration operation.
    /// </summary>
    [MessagePackObject]
    public class GetCommonConfigurationResponse : ApiResponse, IGetCommonConfigurationResponse
    {
        /// <summary>
        /// Gets or sets the common configuration content.
        /// </summary>
        [Key(100)]
        public string CommonConfiguration { get; set; } = string.Empty;
    }
}
