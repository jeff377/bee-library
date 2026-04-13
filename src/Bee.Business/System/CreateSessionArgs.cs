using Bee.Definition.Api;

namespace Bee.Business.System
{
    /// <summary>
    /// Input arguments for creating a user session.
    /// </summary>
    public class CreateSessionArgs : BusinessArgs, ICreateSessionRequest
    {
        /// <summary>
        /// Gets or sets the user account identifier.
        /// </summary>
        public string UserID { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expiration time in seconds. Defaults to 3600.
        /// </summary>
        public int ExpiresIn { get; set; } = 3600;

        /// <summary>
        /// Gets or sets a value indicating whether the session is valid for one-time use only.
        /// </summary>
        public bool OneTime { get; set; } = false;
    }
}
