using System;

namespace Bee.Definition.Api
{
    /// <summary>
    /// Contract interface for create session response data.
    /// </summary>
    public interface ICreateSessionResponse
    {
        /// <summary>
        /// Gets the access token.
        /// </summary>
        Guid AccessToken { get; }

        /// <summary>
        /// Gets the expiration time of the AccessToken in UTC.
        /// </summary>
        DateTime ExpiredAt { get; }
    }
}
