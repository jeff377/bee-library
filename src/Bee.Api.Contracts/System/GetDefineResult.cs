using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Output result for retrieving definition data.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetDefineResult : BusinessResult
    {
        /// <summary>
        /// Gets or sets the definition data as an XML string.
        /// </summary>
        [Key(100)]
        public string Xml { get; set; } = string.Empty;
    }
}
