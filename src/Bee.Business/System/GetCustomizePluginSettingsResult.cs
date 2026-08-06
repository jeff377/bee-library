using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Result of reading one tenant's business plugin bindings.
    /// </summary>
    public class GetCustomizePluginSettingsResult : BusinessResult, IGetCustomizePluginSettingsResponse
    {
        /// <summary>
        /// Gets or sets the bindings as XML, or an empty string when the tenant declares none.
        /// </summary>
        public string Xml { get; set; } = string.Empty;
    }
}
