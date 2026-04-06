using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Input arguments for creating a user session.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class CreateSessionArgs : BusinessArgs
    {
        /// <summary>
        /// Gets or sets the user account identifier.
        /// </summary>
        [Key(100)]
        public string UserID { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expiration time in seconds. Defaults to 3600.
        /// </summary>
        [Key(101)]
        public int ExpiresIn { get; set; } = 3600;

        /// <summary>
        /// Gets or sets a value indicating whether the session is valid for one-time use only.
        /// </summary>
        [Key(102)]
        public bool OneTime { get; set; } = false;
    }
}
