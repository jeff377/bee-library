using Bee.Definition.Api;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for retrieving definition data.
    /// </summary>
    public class GetDefineResult : BusinessResult, IGetDefineResponse
    {
        /// <summary>
        /// Gets or sets the definition data as an XML string.
        /// </summary>
        public string Xml { get; set; } = string.Empty;
    }
}
