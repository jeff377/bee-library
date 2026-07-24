using System.Data;
using System.Globalization;
using Bee.Base;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Settings;

namespace Bee.Business.Form
{
    /// <summary>
    /// Audit-trail half of FormBusinessObject; split out for file size only.
    /// </summary>
    public partial class FormBusinessObject
    {
        #region 異動記錄（audit trail）

        /// <summary>
        /// Whether data-change auditing is enabled (global + change category). Resolved through the
        /// <see cref="IBeeContext.Services"/> escape hatch; false gates out all capture work.
        /// </summary>
        private bool ChangeAuditEnabled()
            => Services.GetService<AuditLogOptions>() is { Enabled: true, ChangeEnabled: true };

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
        /// Serialises the changed rows to a DataSet DiffGram, which carries both the current and the
        /// original (before) values. Plain <c>WriteXml</c> would only write current values.
        /// </summary>
        private static string SerializeDiffGram(DataSet changes)
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            changes.WriteXml(writer, XmlWriteMode.DiffGram);
            return writer.ToString();
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
                    xml = SerializeDiffGram(changes);
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
        /// Resolves the denormalised audit identity (who / company display values) from the session,
        /// so log rows stay self-sufficient without joining the user / company tables.
        /// </summary>
        private (string? userId, string? userName, string? companyId, string? companyName) ResolveAuditIdentity()
        {
            var session = SessionInfoService.Get(AccessToken);
            var companyId = session?.CompanyId;
            string? companyName = null;
            if (!string.IsNullOrEmpty(companyId))
                companyName = Services.GetService<ICompanyInfoService>()?.Get(companyId)?.CompanyName;
            return (session?.UserId, session?.UserName, companyId, companyName);
        }

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
                ProgId = ProgId,
                ChangeTableName = masterTableName,
                RowKey = rowKey,
                ChangeKind = changeKind,
                IsSensitive = false,
                ChangesXml = changesXml,
                Source = source,
            });
        }

        /// <summary>
        /// Whether read/access auditing is enabled (global + access category).
        /// </summary>
        private bool AccessAuditEnabled()
            => Services.GetService<AuditLogOptions>() is { Enabled: true, AccessEnabled: true };

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
                ProgId = ProgId,
                RowKey = rowId.ToString(),
                Source = source,
            });
        }

        #endregion
    }
}
