namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for get common configuration response data.
    /// </summary>
    public interface IGetCommonConfigurationResponse
    {
        /// <summary>
        /// Gets the common configuration content.
        /// </summary>
        string CommonConfiguration { get; }
    }
}
