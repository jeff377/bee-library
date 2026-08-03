using Bee.Api.Contracts.System;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for enabling or disabling an issued API key.
    /// </summary>
    public class SetApiKeyEnabledResult : BusinessResult, ISetApiKeyEnabledResponse
    {
        /// <summary>
        /// Gets or sets the key identifier whose state was set.
        /// </summary>
        public string SysId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the state now stored for the key.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
