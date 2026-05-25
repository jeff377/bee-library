using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for retrieving a form layout as a typed object.
    /// </summary>
    public class GetFormLayoutArgs : BusinessArgs, IGetFormLayoutRequest
    {
        /// <summary>
        /// Gets or sets the program identifier whose layout should be retrieved.
        /// </summary>
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the layout identifier; empty string resolves to the default layout.
        /// </summary>
        public string LayoutId { get; set; } = string.Empty;
    }
}
