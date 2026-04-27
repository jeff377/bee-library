using Bee.Api.Contracts;
using MessagePack;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API response for the get common configuration operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetCommonConfigurationResponse : ApiResponse, IGetCommonConfigurationResponse
    {
        /// <summary>
        /// Gets or sets the common configuration content.
        /// </summary>
        [Key(100)]
        public string CommonConfiguration { get; set; } = string.Empty;
    }
}
