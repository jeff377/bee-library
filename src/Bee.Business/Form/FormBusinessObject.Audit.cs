using System.Data;
using Bee.Base;
using Bee.Business.AuditLog;
using Bee.Definition;
using Bee.Definition.Logging;
using Bee.Definition.Settings;

namespace Bee.Business.Form
{
    /// <summary>
    /// Audit-trail half of FormBusinessObject; split out for file size only.
    /// </summary>
    public partial class FormBusinessObject
    {
        #region Audit trail

        /// <summary>
        /// Whether data-change auditing applies to this form. Resolved through the
        /// <see cref="IBeeContext.Services"/> escape hatch; false gates out all capture work.
        /// </summary>
        /// <remarks>
        /// Two levels, and only the first is a gate: <see cref="AuditLogOptions.Enabled"/> switches
        /// the whole subsystem off — short-circuiting before any rule lookup, so a deployment not
        /// using the audit trail pays nothing — while
        /// <see cref="AuditLogOptions.ChangeEnabled"/> supplies the value an
        /// <see cref="AuditRuleMode.Inherit"/> rule defers to. A form declaring
        /// <see cref="AuditRuleMode.On"/> is therefore recorded even where the deployment default
        /// is off, which is the point of per-form rules.
        /// </remarks>
        private bool ChangeAuditEnabled()
        {
            var options = Services.GetService<AuditLogOptions>();
            if (options is not { Enabled: true }) { return false; }

            var rule = ResolveAuditRule();
            return rule == null
                ? options.ChangeEnabled
                : rule.ChangeMode.Resolve(options.ChangeEnabled);
        }

        /// <summary>
        /// Reads the master row's key and derives the <see cref="ChangeKind"/> from its state. Must be
        /// called before <c>Save</c> applies the changes (which resets RowState).
        /// </summary>
        private static (string? rowKey, ChangeKind kind) ExtractMasterChange(DataSet dataSet, string masterTableName)
        {
            if (string.IsNullOrEmpty(masterTableName) || !dataSet.Tables.Contains(masterTableName))
                return (null, ChangeKind.Update);

            var table = dataSet.Tables[masterTableName]!;
            if (table.Rows.Count == 0 || !table.Columns.Contains(SysFields.RowId))
                return (null, ChangeKind.Update);

            var row = table.Rows[0];
            var kind = row.RowState switch
            {
                DataRowState.Added => ChangeKind.Insert,
                DataRowState.Deleted => ChangeKind.Delete,
                _ => ChangeKind.Update,
            };
            var version = row.RowState == DataRowState.Deleted ? DataRowVersion.Original : DataRowVersion.Current;
            return (ValueUtilities.CStr(row[SysFields.RowId, version]), kind);
        }

        /// <summary>
        /// Writes the delete audit. When the pre-delete <paramref name="snapshot"/> is available its
        /// rows are marked deleted and serialised as a DiffGram before-image (full deleted content);
        /// otherwise the deleted key alone is recorded.
        /// </summary>
        private void WriteDeleteAudit(DataSet? snapshot, Guid rowId)
        {
            var masterTableName = DefineAccess.GetFormSchema(ProgId).MasterTable?.TableName ?? string.Empty;
            var rowKey = rowId.ToString();

            string xml = MinimalDeleteXml(masterTableName, rowKey);
            if (snapshot != null && HasAnyRows(snapshot))
            {
                MarkAllRowsDeleted(snapshot);
                using var changes = snapshot.GetChanges();
                if (changes != null)
                    xml = AuditDiffGram.Serialize(changes);
            }

            WriteChangeAudit(ChangeKind.Delete, rowKey, xml, masterTableName, ProgId + ".Delete");
        }

        /// <summary>Marks every row in every table as deleted so <c>GetChanges</c> yields the before-image.</summary>
        private static void MarkAllRowsDeleted(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                // Iterate backwards: Delete() on an Added row removes it immediately; loaded rows are
                // Unchanged so this is defensive.
                for (int i = table.Rows.Count - 1; i >= 0; i--)
                {
                    var row = table.Rows[i];
                    if (row.RowState != DataRowState.Deleted)
                        row.Delete();
                }
            }
        }

        private static bool HasAnyRows(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                if (table.Rows.Count > 0)
                    return true;
            }
            return false;
        }

        private static string MinimalDeleteXml(string masterTableName, string rowKey)
            => $"<DeletedRow table=\"{masterTableName}\" sys_rowid=\"{rowKey}\" />";

        /// <summary>
        /// Builds a <see cref="ChangeAuditEntry"/> from the session (denormalised who / company) and
        /// the supplied change payload, and writes it best-effort through <see cref="IAuditLogWriter"/>.
        /// </summary>
        private void WriteChangeAudit(ChangeKind changeKind, string? rowKey, string changesXml, string masterTableName, string source)
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
                ProgId = ProgId,
                ChangeTableName = masterTableName,
                RowKey = rowKey,
                ChangeKind = changeKind,
                IsSensitive = ResolveAuditRule()?.IsSensitive ?? false,
                ChangesXml = changesXml,
                Source = source,
            });
        }

        /// <summary>
        /// Whether read/access auditing applies to this form.
        /// </summary>
        /// <remarks>
        /// Same two levels as <see cref="ChangeAuditEnabled"/>. This axis is where per-form rules
        /// matter most: <see cref="AuditLogOptions.AccessEnabled"/> defaults to off because read
        /// volume is high, so recording views of one sensitive form is only expressible as a rule.
        /// </remarks>
        private bool AccessAuditEnabled()
        {
            var options = Services.GetService<AuditLogOptions>();
            if (options is not { Enabled: true }) { return false; }

            var rule = ResolveAuditRule();
            return rule == null
                ? options.AccessEnabled
                : rule.AccessMode.Resolve(options.AccessEnabled);
        }

        /// <summary>
        /// Gets this form's audit rule for the session's company, or <c>null</c> when the form has
        /// none — which means every axis inherits the deployment default.
        /// </summary>
        /// <remarks>
        /// No company entered also yields <c>null</c>, and correctly so: the rules live in a company
        /// database, so there is nothing to read before one is chosen.
        /// <para>
        /// Reads the company id straight from the session rather than through
        /// <c>ResolveAuditIdentity</c>, which additionally resolves the company <i>name</i> — work
        /// this gate has no use for and would pay on every save and every record view.
        /// </para>
        /// </remarks>
        private AuditRule? ResolveAuditRule()
        {
            string? companyId = SessionInfoService.Get(AccessToken)?.CompanyId;
            if (string.IsNullOrEmpty(companyId)) { return null; }

            return Services.GetService<IAuditRuleService>()?.Get(companyId)?.Find(ProgId);
        }

        /// <summary>
        /// Writes an <see cref="AccessAuditEntry"/> recording that the given record was viewed
        /// (who + prog_id + row_key), best-effort through <see cref="IAuditLogWriter"/>.
        /// </summary>
        private void WriteAccessAudit(Guid rowId, string source)
        {
            var (userId, userName, companyId, companyName) = ResolveAuditIdentity();
            Services.GetService<IAuditLogWriter>()?.Write(new AccessAuditEntry
            {
                UserId = userId,
                UserName = userName,
                CompanyId = companyId,
                CompanyName = companyName,
                AccessToken = AccessToken,
                ApiKeyId = ApiKeyId,
                ApiKeyName = ApiKeyName,
                ProgId = ProgId,
                RowKey = rowId.ToString(),
                Source = source,
            });
        }

        #endregion
    }
}
