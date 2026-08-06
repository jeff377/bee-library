namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the response to storing a tenant's business plugin bindings.
    /// </summary>
    public interface ISaveCustomizePluginSettingsResponse
    {
        /// <summary>
        /// Gets the number of plugin bindings stored, across every program in the definition.
        /// </summary>
        /// <remarks>
        /// Confirms what the store actually accepted, which matters because the save validates
        /// every bound type and refuses the whole definition if one does not hold up.
        /// </remarks>
        int PluginCount { get; }
    }
}
