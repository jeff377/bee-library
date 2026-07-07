namespace Bee.Definition.Logging
{
    /// <summary>
    /// The kind of login-related event recorded in <c>st_log_login</c>. The value encodes the
    /// outcome directly (success / failure / lockout / logout), so no separate result column is
    /// needed — aligning with how SAP Security Audit Log message ids and Odoo <c>res.users.log</c>
    /// model login events.
    /// </summary>
    public enum LoginEvent
    {
        /// <summary>A successful login that created a session.</summary>
        LoginSucceeded,

        /// <summary>A login rejected because the credentials were invalid.</summary>
        LoginFailed,

        /// <summary>A login rejected because the account was temporarily locked.</summary>
        LockedOut,

        /// <summary>A logout that destroyed the session.</summary>
        Logout,
    }

    /// <summary>
    /// Audit entry for a login-axis event (row in <c>st_log_login</c>).
    /// </summary>
    public sealed class LoginAuditEntry : AuditEntry
    {
        /// <inheritdoc/>
        public override string TableName => "st_log_login";

        /// <summary>Gets the login event kind (encodes the outcome).</summary>
        public LoginEvent Event { get; init; }

        /// <summary>Gets the failure reason for a failed / locked-out event; never a plaintext password.</summary>
        public string? FailReason { get; init; }

        /// <inheritdoc/>
        protected override void AddColumns(IList<AuditColumn> columns)
        {
            columns.Add(new AuditColumn("event", (int)Event));
            columns.Add(new AuditColumn("fail_reason", FailReason));
        }
    }
}
