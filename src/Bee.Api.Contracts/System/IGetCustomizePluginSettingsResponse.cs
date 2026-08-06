namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the response carrying one tenant's business plugin bindings.
    /// </summary>
    public interface IGetCustomizePluginSettingsResponse
    {
        /// <summary>
        /// Gets the bindings as XML, or an empty string when the tenant declares none.
        /// </summary>
        /// <remarks>
        /// XML rather than the object itself, matching how definitions travel elsewhere. The
        /// bindings are held in get-only collection properties, which a .NET client cannot
        /// reconstruct from a deserialized object graph — silently, with no error.
        /// </remarks>
        string Xml { get; }
    }
}
