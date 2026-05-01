using System.ComponentModel;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// User data stored in session info.
    /// Retains the information needed to reconstruct a <see cref="SessionInfo"/>; this data is persisted in the database.
    /// </summary>
    public class SessionUser
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the user account ID.
        /// </summary>
        public string UserID { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the session expiration time.
        /// </summary>
        [DefaultValue(typeof(DateTime), "0001-01-01T00:00:00.0000000Z")]
        public DateTime EndTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets a value indicating whether this token is one-time use only.
        /// </summary>
        [DefaultValue(false)]
        public bool OneTime { get; set; } = false;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{UserID} : {UserName}";
        }
    }
}
