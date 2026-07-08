using System.Data;
using Bee.Base;
using Bee.Api.Contracts;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Audit-log business object (<c>AuditLog</c> axis): read-only queries over the <c>st_log_*</c>
    /// audit tables in the log database. Every action is gated behind the <c>AuditLog</c> permission
    /// model so a general user cannot read another's trail, and no action mutates the append-only log.
    /// </summary>
    public class LogBusinessObject : BusinessObject, ILogBusinessObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogBusinessObject"/> class.
        /// </summary>
        /// <param name="ctx">The per-call context aggregating cross-cutting services.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public LogBusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall = true)
            : base(ctx, accessToken, isLocalCall)
        { }

        /// <summary>
        /// Gets a record's change history: every <c>st_log_change</c> event for the given
        /// <c>ProgId</c> + <c>RowKey</c>, newest first, with each event's <c>changes_xml</c> DiffGram
        /// restored into structured before/after field values.
        /// </summary>
        /// <param name="args">The input arguments carrying the target <c>ProgId</c> and <c>RowKey</c>.</param>
        /// <remarks>
        /// Requires the <see cref="PermissionAction.Read"/> grant on the <c>AuditLog</c> permission
        /// model. The result is restricted to the caller's current company (the denormalised
        /// <c>company_id</c> on the log row), so a user only sees their own company's trail.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual GetRecordHistoryResult GetRecordHistory(GetRecordHistoryArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.ProgId))
                throw new ArgumentException("ProgId is required.", nameof(args));
            if (string.IsNullOrWhiteSpace(args.RowKey))
                throw new ArgumentException("RowKey is required.", nameof(args));

            // Permission gate: only roles granted audit-read may query the trail.
            var authorization = Services.GetRequiredService<IAuthorizationService>();
            if (!authorization.Can(AccessToken, SysProgIds.AuditLog, PermissionAction.Read))
                throw new UnauthorizedAccessException("Not authorized to read the audit log.");

            // Scope to the caller's current company so cross-company trails are not exposed.
            var companyId = SessionInfoService.Get(AccessToken)?.CompanyId;

            var repository = Services.GetRequiredService<IAuditLogRepositoryFactory>().CreateAuditLogRepository();
            var table = repository.GetRecordChangeHistory(args.ProgId, args.RowKey, companyId);

            var changes = new List<RecordHistoryEntry>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                changes.Add(BuildEntry(row));
            }

            return new GetRecordHistoryResult
            {
                ProgId = args.ProgId,
                RowKey = args.RowKey,
                Changes = changes,
            };
        }

        /// <summary>
        /// Maps one <c>st_log_change</c> row to a <see cref="RecordHistoryEntry"/>, restoring its
        /// DiffGram payload into field-level before/after changes.
        /// </summary>
        private static RecordHistoryEntry BuildEntry(DataRow row)
        {
            return new RecordHistoryEntry
            {
                SysRowId = ValueUtilities.CGuid(row["sys_rowid"]),
                LogTime = ReadUtc(row["log_time"]),
                UserId = ReadNullableString(row["user_id"]),
                UserName = ReadNullableString(row["user_name"]),
                ChangeKind = (ChangeKind)ValueUtilities.CInt(row["change_kind"]),
                IsSensitive = ValueUtilities.CBool(row["is_sensitive"]),
                Source = ReadNullableString(row["source"]),
                Fields = ChangeDiffGramReader.Read(ReadNullableString(row["changes_xml"])),
            };
        }

        /// <summary>Reads a log-time column as a UTC <see cref="DateTime"/> (the write side stores UTC).</summary>
        private static DateTime ReadUtc(object value)
            => DateTime.SpecifyKind(ValueUtilities.CDateTime(value), DateTimeKind.Utc);

        /// <summary>Reads a nullable string column, mapping <see cref="DBNull"/> to <c>null</c>.</summary>
        private static string? ReadNullableString(object value)
            => value is null or DBNull ? null : ValueUtilities.CStr(value);
    }
}
