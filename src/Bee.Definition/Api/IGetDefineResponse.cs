namespace Bee.Definition.Api
{
    /// <summary>
    /// Contract interface for get definition response data.
    /// </summary>
    public interface IGetDefineResponse
    {
        /// <summary>
        /// Gets the definition XML content.
        /// </summary>
        string Xml { get; }
    }
}
