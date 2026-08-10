using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for storing one tenant's business plugin bindings.
    /// </summary>
    public class SaveCustomizePluginSettingsResponse : ApiResponse, ISaveCustomizePluginSettingsResponse
    {
        /// <summary>
        /// Gets or sets the number of plugin bindings stored, across every program in the definition.
        /// </summary>
        public int PluginCount { get; set; }
    }
}
