using Bee.Base;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Business.AuditLog
{
    /// <summary>
    /// Audit-log business object (<c>AuditLog</c> axis): read-only queries over the <c>st_log_*</c>
    /// audit tables in the log database. Every action is gated behind the <c>AuditLog</c> permission
    /// model so a general user cannot read another's trail, results are scoped to the caller's current
    /// company, and no action mutates the append-only log.
    /// </summary>
    /// <remarks>
    /// The change axis follows a list / detail split: <see cref="GetChangeLog"/> returns lightweight
    /// event headers (no DiffGram), and <see cref="GetChangeDetail"/> restores one event's
    /// <c>changes_xml</c> into structured before/after values on demand.
    /// </remarks>
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
        /// Gets a filtered, paged list of <c>st_log_change</c> event headers across records.
        /// </summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        /// <remarks>
        /// Typical uses: a form's changes over a period (<c>ProgId</c> + time range), or a user's changes
        /// over a period (<c>UserId</c> + time range). Setting <c>ProgId</c> + <c>RowKey</c> narrows it to a
        /// single record's history. Field-level before/after detail for any listed row is fetched via
        /// <see cref="GetChangeDetail"/>.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual GetChangeLogResult GetChangeLog(GetChangeLogArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            EnsureAuditReadAllowed();

            var query = new ChangeLogQuery
            {
                FromUtc = args.FromUtc,
                ToUtc = args.ToUtc,
                UserId = args.UserId,
                ProgId = args.ProgId,
                RowKey = args.RowKey,
                ChangeKind = args.ChangeKind,
                CompanyId = CurrentCompanyId(),
            };
            var page = Repository().GetChangeLog(query, args.Paging ?? new PagingOptions());

            return new GetChangeLogResult { Table = page.Table, Paging = page.Paging };
        }

        /// <summary>
        /// Gets one change event's restored field-level before/after detail, by its log row id.
        /// </summary>
        /// <param name="args">The input arguments carrying the event's <c>SysRowId</c>.</param>
        /// <remarks>
        /// Throws <see cref="InvalidOperationException"/> when no such event exists within the caller's
        /// company scope (a listed row that has since been partitioned away, or an out-of-scope id).
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual GetChangeDetailResult GetChangeDetail(GetChangeDetailArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (args.SysRowId == Guid.Empty)
                throw new ArgumentException("SysRowId is required.", nameof(args));

            EnsureAuditReadAllowed();

            var table = Repository().GetChangeById(args.SysRowId, CurrentCompanyId())
                ?? throw new InvalidOperationException("Change record not found.");
            var row = table.Rows[0];

            return new GetChangeDetailResult
            {
                SysRowId = ValueUtilities.CGuid(row["sys_rowid"]),
                LogTime = ReadUtc(row["log_time"]),
                UserId = ReadNullableString(row["user_id"]),
                UserName = ReadNullableString(row["user_name"]),
                ProgId = ReadNullableString(row["prog_id"]),
                RowKey = ReadNullableString(row["row_key"]),
                ChangeKind = (ChangeKind)ValueUtilities.CInt(row["change_kind"]),
                IsSensitive = ValueUtilities.CBool(row["is_sensitive"]),
                Source = ReadNullableString(row["source"]),
                Fields = ChangeDiffGramReader.Read(ReadNullableString(row["changes_xml"])),
            };
        }

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_login</c> event headers.
        /// </summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual LogListResult GetLoginLog(GetLoginLogArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            EnsureAuditReadAllowed();

            var query = new LoginLogQuery
            {
                FromUtc = args.FromUtc,
                ToUtc = args.ToUtc,
                UserId = args.UserId,
                Event = args.Event,
                CompanyId = CurrentCompanyId(),
            };
            return ToResult(Repository().GetLoginLog(query, args.Paging ?? new PagingOptions()));
        }

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_access</c> record-view headers.
        /// </summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual LogListResult GetAccessLog(GetAccessLogArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            EnsureAuditReadAllowed();

            var query = new AccessLogQuery
            {
                FromUtc = args.FromUtc,
                ToUtc = args.ToUtc,
                UserId = args.UserId,
                ProgId = args.ProgId,
                RowKey = args.RowKey,
                CompanyId = CurrentCompanyId(),
            };
            return ToResult(Repository().GetAccessLog(query, args.Paging ?? new PagingOptions()));
        }

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_anomaly_api</c> API-anomaly headers.
        /// </summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual LogListResult GetApiAnomalyLog(GetApiAnomalyLogArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            EnsureAuditReadAllowed();

            var query = new ApiAnomalyLogQuery
            {
                FromUtc = args.FromUtc,
                ToUtc = args.ToUtc,
                UserId = args.UserId,
                Method = args.Method,
                Kind = args.Kind,
                CompanyId = CurrentCompanyId(),
            };
            return ToResult(Repository().GetApiAnomalyLog(query, args.Paging ?? new PagingOptions()));
        }

        /// <summary>
        /// Gets a filtered, paged list of <c>st_log_anomaly_db</c> DB-anomaly headers.
        /// </summary>
        /// <param name="args">The input arguments carrying the typed filter and optional paging.</param>
        /// <remarks>
        /// <c>st_log_anomaly_db</c> carries no company, so this is a cross-company infrastructure view;
        /// it is still gated behind the <c>AuditLog</c> read permission.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual LogListResult GetDbAnomalyLog(GetDbAnomalyLogArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            EnsureAuditReadAllowed();

            var query = new DbAnomalyLogQuery
            {
                FromUtc = args.FromUtc,
                ToUtc = args.ToUtc,
                DatabaseId = args.DatabaseId,
                Kind = args.Kind,
            };
            return ToResult(Repository().GetDbAnomalyLog(query, args.Paging ?? new PagingOptions()));
        }

        /// <summary>
        /// Gets API-anomaly counts grouped by anomaly kind over an optional time window (monitoring summary).
        /// </summary>
        /// <param name="args">The input arguments carrying the optional time window.</param>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual LogAggregateResult GetApiAnomalySummary(GetApiAnomalySummaryArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            EnsureAuditReadAllowed();
            var table = Repository().GetApiAnomalySummary(args.FromUtc, args.ToUtc, CurrentCompanyId());
            return new LogAggregateResult { Table = table };
        }

        /// <summary>
        /// Gets DB-anomaly counts grouped by anomaly kind over an optional time window (monitoring summary).
        /// </summary>
        /// <param name="args">The input arguments carrying the optional time window.</param>
        /// <remarks>
        /// <c>st_log_anomaly_db</c> carries no company, so this is a cross-company infrastructure summary;
        /// it is still gated behind the <c>AuditLog</c> read permission.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual LogAggregateResult GetDbAnomalySummary(GetDbAnomalySummaryArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            EnsureAuditReadAllowed();
            var table = Repository().GetDbAnomalySummary(args.FromUtc, args.ToUtc);
            return new LogAggregateResult { Table = table };
        }

        /// <summary>
        /// Gets the top API methods by anomaly count over an optional time window (monitoring hot-spots).
        /// </summary>
        /// <param name="args">The input arguments carrying the optional time window and top-N.</param>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual LogAggregateResult GetTopApiMethods(GetTopApiMethodsArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            EnsureAuditReadAllowed();
            var table = Repository().GetTopApiMethods(args.FromUtc, args.ToUtc, args.TopN, CurrentCompanyId());
            return new LogAggregateResult { Table = table };
        }

        /// <summary>Maps a repository <see cref="AuditLogPage"/> to the shared list result.</summary>
        private static LogListResult ToResult(AuditLogPage page)
            => new LogListResult { Table = page.Table, Paging = page.Paging };

        /// <summary>Resolves the log-scoped repository from the DI escape hatch.</summary>
        private IAuditLogRepository Repository()
            => Services.GetRequiredService<IAuditLogRepositoryFactory>().CreateAuditLogRepository();

        /// <summary>Resolves the caller's current company id (denormalised query scope); null when none.</summary>
        private string? CurrentCompanyId() => SessionInfoService.Get(AccessToken)?.CompanyId;

        /// <summary>
        /// Enforces the audit-read permission gate: only roles granted <see cref="PermissionAction.Read"/>
        /// on the <c>AuditLog</c> model may query the trail.
        /// </summary>
        private void EnsureAuditReadAllowed()
        {
            var authorization = Services.GetRequiredService<IAuthorizationService>();
            if (!authorization.Can(AccessToken, SysProgIds.AuditLog, PermissionAction.Read))
                throw new UnauthorizedAccessException("Not authorized to read the audit log.");
        }

        /// <summary>Reads a log-time column as a UTC <see cref="DateTime"/> (the write side stores UTC).</summary>
        private static DateTime ReadUtc(object value)
            => DateTime.SpecifyKind(ValueUtilities.CDateTime(value), DateTimeKind.Utc);

        /// <summary>Reads a nullable string column, mapping <see cref="DBNull"/> to <c>null</c>.</summary>
        private static string? ReadNullableString(object value)
            => value is null or DBNull ? null : ValueUtilities.CStr(value);
    }
}
