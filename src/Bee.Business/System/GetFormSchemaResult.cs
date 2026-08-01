using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for retrieving a form schema as a typed object.
    /// </summary>
    public class GetFormSchemaResult : BusinessResult, IGetFormSchemaResponse
    {
        /// <summary>
        /// Gets or sets the raw definition serialised as XML; empty when no definition exists.
        /// </summary>
        /// <remarks>
        /// Every definition-fetching API carries XML. Definition types declare XML as their
        /// serialisation contract — their nested collections are get-only, which XmlSerializer
        /// handles by populating the existing instance, while JSON and MessagePack bind by
        /// writability and would silently drop those collections on the way back.
        /// </remarks>
        public string? Xml { get; set; }
    }
}
