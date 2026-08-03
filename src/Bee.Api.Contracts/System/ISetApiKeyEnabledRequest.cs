namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the enable / disable API key request.
    /// </summary>
    public interface ISetApiKeyEnabledRequest
    {
        /// <summary>
        /// Gets the key identifier (<c>st_api_key.sys_id</c>) being enabled or disabled.
        /// </summary>
        string SysId { get; }

        /// <summary>
        /// Gets a value indicating whether the key is accepted from now on.
        /// </summary>
        bool Enabled { get; }
    }
}
