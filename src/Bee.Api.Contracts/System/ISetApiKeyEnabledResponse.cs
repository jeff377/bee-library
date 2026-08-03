namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the enable / disable API key response.
    /// </summary>
    public interface ISetApiKeyEnabledResponse
    {
        /// <summary>
        /// Gets the key identifier whose state was set.
        /// </summary>
        string SysId { get; }

        /// <summary>
        /// Gets the state now stored for the key.
        /// </summary>
        bool Enabled { get; }
    }
}
