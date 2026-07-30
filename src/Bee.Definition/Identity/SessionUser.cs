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
        /// Gets or sets the company the session is currently working in, or <c>null</c> when it
        /// has not entered one.
        /// </summary>
        /// <remarks>
        /// The only company-scoped value in the seed, because it is the only one that cannot be
        /// derived: nothing in the database records which company the user picked. Everything else
        /// <c>EnterCompany</c> snapshots onto the session (roles, customization code, record-scope
        /// row ids) is recomputed on rebuild, which is also what keeps a revoked permission from
        /// surviving in a stale snapshot.
        ///
        /// A seed written before this property existed deserializes to <c>null</c>, rebuilding as
        /// a signed-in session that has not entered a company — the same state
        /// <c>LeaveCompany</c> leaves behind.
        /// </remarks>
        public string? CompanyId { get; set; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{UserID} : {UserName}";
        }
    }
}
