namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the request that reads one tenant's business plugin bindings.
    /// </summary>
    public interface IGetCustomizePluginSettingsRequest
    {
        /// <summary>
        /// Gets the tenant customization code whose bindings are requested.
        /// </summary>
        string CustomizeId { get; }
    }
}
