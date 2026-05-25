using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the get form layout operation.
    /// </summary>
    [MessagePackObject]
    public class GetFormLayoutRequest : ApiRequest, IGetFormLayoutRequest
    {
        /// <summary>
        /// Gets or sets the program identifier whose layout should be retrieved.
        /// </summary>
        [Key(100)]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the layout identifier; empty string resolves to the default layout.
        /// </summary>
        [Key(101)]
        public string LayoutId { get; set; } = string.Empty;
    }
}
