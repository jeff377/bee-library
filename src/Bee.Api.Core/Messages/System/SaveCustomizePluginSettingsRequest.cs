using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for storing one tenant's business plugin bindings.
    /// </summary>
    public class SaveCustomizePluginSettingsRequest : ApiRequest, ISaveCustomizePluginSettingsRequest
    {
        /// <summary>
        /// Gets or sets the tenant customization code whose bindings are being stored.
        /// </summary>
        public string CustomizeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the bindings as XML. Replaces the tenant's bindings outright.
        /// </summary>
        public string Xml { get; set; } = string.Empty;
    }
}
