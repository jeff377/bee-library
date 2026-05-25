namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for the get form schema request.
    /// </summary>
    public interface IGetFormSchemaRequest
    {
        /// <summary>
        /// Gets the program identifier of the form schema to retrieve.
        /// </summary>
        string ProgId { get; }
    }
}
