using Bee.Definition;
using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for saving definition data.
    /// </summary>
    public class SaveDefineArgs : BusinessArgs, ISaveDefineRequest
    {
        /// <summary>
        /// Gets or sets the definition data type.
        /// </summary>
        public DefineType DefineType { get; set; }

        /// <summary>
        /// Gets or sets the definition data as an XML string.
        /// </summary>
        public string Xml { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the keys used to locate where the definition data is saved.
        /// </summary>
        public string[] Keys { get; set; } = null;
    }
}
