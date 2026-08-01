namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the get form schema response.
    /// </summary>
    public interface IGetFormSchemaResponse
    {
        /// <summary>
        /// Gets the raw definition serialised as XML; empty when no definition exists.
        /// </summary>
        /// <remarks>
        /// Every definition-fetching API carries XML. Definition types declare XML as their
        /// serialisation contract — their nested collections are get-only, which XmlSerializer
        /// handles by populating the existing instance, while JSON and MessagePack bind by
        /// writability and would silently drop those collections on the way back.
        /// </remarks>
        string? Xml { get; }
    }
}
