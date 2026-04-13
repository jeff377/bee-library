using System;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API response for the get definition operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetDefineResponse : ApiResponse, IGetDefineResponse
    {
        /// <summary>
        /// Gets or sets the definition XML content.
        /// </summary>
        [Key(100)]
        public string Xml { get; set; } = string.Empty;
    }
}
