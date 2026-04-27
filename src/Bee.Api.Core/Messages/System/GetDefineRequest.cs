using Bee.Definition;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the get definition operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetDefineRequest : ApiRequest, IGetDefineRequest
    {
        /// <summary>
        /// Gets or sets the definition type.
        /// </summary>
        [Key(100)]
        public DefineType DefineType { get; set; }

        /// <summary>
        /// Gets or sets the optional filter keys.
        /// </summary>
        [Key(101)]
        public string[]? Keys { get; set; } = null;
    }
}
