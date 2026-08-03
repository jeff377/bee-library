using Bee.Definition;
using Bee.Definition.Logging;
using Bee.Definition.Settings;

namespace Bee.Business.System
{
    /// <summary>
    /// Audit-trail half of <see cref="SystemBusinessObject"/> for deployment-level operations.
    /// Split out for file size only.
    /// </summary>
    public partial class SystemBusinessObject
    {
        /// <summary>
        /// Whether deployment-level operations are recorded.
        /// </summary>
        /// <remarks>
        /// NOTE: gated on <see cref="AuditLogOptions.Enabled"/> alone — deliberately <b>not</b> on
        /// <see cref="AuditLogOptions.ChangeEnabled"/>, which the data-change path also requires.
        /// The asymmetry is the point: <c>ChangeEnabled</c> exists so a deployment can opt out of
        /// the volume of ordinary business-data history, and appointing an administrator or minting
        /// a credential is neither ordinary nor voluminous. A deployment that has auditing on at all
        /// gets these, and cannot turn them off without turning off the audit trail entirely.
        /// </remarks>
        private bool DeploymentAuditEnabled()
            => Services.GetService<AuditLogOptions>() is { Enabled: true };

        /// <summary>
        /// Records a deployment-level operation on the change axis, best-effort through
        /// <see cref="IAuditLogWriter"/>.
        /// </summary>
        /// <param name="tableName">The affected table.</param>
        /// <param name="rowKey">The affected row's key.</param>
        /// <param name="changeKind">Whether the row was inserted or updated.</param>
        /// <param name="changesXml">The DiffGram payload carrying the before/after values.</param>
        /// <param name="action">The operation name, appended to the system prog id as the source.</param>
        /// <remarks>
        /// Entries land in <c>st_log_change</c> under <see cref="SysProgIds.System"/> rather than in
        /// an axis of their own: the shape being recorded — who changed which row of which table,
        /// from what to what — is exactly the change axis, and a separate table would need its own
        /// query API and its own place in every audit UI to say the same thing.
        /// <para>
        /// They are always marked sensitive. Both operations grant capability rather than record
        /// business data, so a deployment filtering its log for what matters should always see them.
        /// </para>
        /// <para>
        /// <see cref="AuditEntry.CompanyId"/> is normally empty here, and that is correct: a
        /// deployment-level operation belongs to no company. An operator who happens to have entered
        /// one has it recorded as context, not as the scope of the change.
        /// </para>
        /// </remarks>
        private void WriteDeploymentAudit(string tableName, string? rowKey, ChangeKind changeKind,
            string changesXml, string action)
        {
            var (userId, userName, companyId, companyName) = ResolveAuditIdentity();
            Services.GetService<IAuditLogWriter>()?.Write(new ChangeAuditEntry
            {
                UserId = userId,
                UserName = userName,
                CompanyId = companyId,
                CompanyName = companyName,
                AccessToken = AccessToken,
                ApiKeyId = ApiKeyId,
                ApiKeyName = ApiKeyName,
                ProgId = SysProgIds.System,
                ChangeTableName = tableName,
                RowKey = rowKey,
                ChangeKind = changeKind,
                IsSensitive = true,
                ChangesXml = changesXml,
                Source = SysProgIds.System + "." + action,
            });
        }
    }
}
