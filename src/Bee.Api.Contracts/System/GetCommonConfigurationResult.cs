using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Output result for retrieving common parameters and environment configuration.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetCommonConfigurationResult : BusinessResult
    {
        /// <summary>
        /// Gets or sets the common parameters and environment configuration.
        /// </summary>
        [Key(100)]
        public string CommonConfiguration { get; set; } = string.Empty;
    }
}
