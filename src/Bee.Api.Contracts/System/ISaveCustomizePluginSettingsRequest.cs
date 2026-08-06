namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the request that stores one tenant's business plugin bindings.
    /// </summary>
    public interface ISaveCustomizePluginSettingsRequest
    {
        /// <summary>
        /// Gets the tenant customization code whose bindings are being stored.
        /// </summary>
        string CustomizeId { get; }

        /// <summary>
        /// Gets the bindings as XML. Replaces the tenant's bindings outright.
        /// </summary>
        string Xml { get; }
    }
}
