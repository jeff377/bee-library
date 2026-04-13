using Bee.Definition;
using Bee.Definition.Api;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for retrieving definition data.
    /// </summary>
    public class GetDefineArgs : BusinessArgs, IGetDefineRequest
    {
        /// <summary>
        /// Gets or sets the definition data type.
        /// </summary>
        public DefineType DefineType { get; set; }

        /// <summary>
        /// Gets or sets the keys used to locate the definition data.
        /// </summary>
        public string[] Keys { get; set; } = null;
    }
}
