namespace Bee.Definition.Logging
{
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
