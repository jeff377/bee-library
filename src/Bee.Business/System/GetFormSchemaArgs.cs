using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for retrieving a form schema as a typed object.
    /// </summary>
    public class GetFormSchemaArgs : BusinessArgs, IGetFormSchemaRequest
    {
        /// <summary>
        /// Gets or sets the program identifier of the form schema to retrieve.
        /// </summary>
        public string ProgId { get; set; } = string.Empty;
    }
}
