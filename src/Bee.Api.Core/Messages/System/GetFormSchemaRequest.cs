using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the get form schema operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetFormSchemaRequest : ApiRequest, IGetFormSchemaRequest
    {
        /// <summary>
        /// Gets or sets the program identifier of the form schema to retrieve.
        /// </summary>
        public string ProgId { get; set; } = string.Empty;
    }
}
