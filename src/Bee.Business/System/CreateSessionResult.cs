using System;
using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for creating a user session.
    /// </summary>
    public class CreateSessionResult : BusinessResult, ICreateSessionResponse
    {
        /// <summary>
        /// Gets or sets the access token used for authenticating subsequent API calls.
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the expiration time of the AccessToken in UTC.
        /// </summary>
        public DateTime ExpiredAt { get; set; }
    }
}
