using System.Globalization;
using Bee.Db;
using Bee.Definition.Database;
using Bee.ObjectCaching;
using Bee.ObjectCaching.CacheNotify;

namespace Bee.Hosting.CacheNotify
{
    /// <summary>
    /// The pollable core of the cache-notify mechanism for a single database: holds an in-memory
    /// mirror of <c>{cache_key → version}</c> and a high-water mark over <c>sys_update_time</c>,
    /// and on each <see cref="Poll"/> reads the incremental delta and routes evictions.
    /// </summary>
    /// <remarks>
    /// Separated from the hosted-service timer shell (<see cref="CacheNotifyPoller"/>) so the
    /// polling logic can be driven deterministically in tests. Not thread-safe: a single poller
    /// loop owns one instance and calls <see cref="Poll"/> sequentially.
    /// <para>
    /// Incremental by <c>sys_update_time</c> (indexed) so a large multi-tenant notification table
    /// is not scanned in full every cycle. The first call only establishes the baseline high-water
    /// mark (<c>max(sys_update_time)</c>, or the server's current time when the table is empty) and
    /// evicts nothing — historical rows are stale for a just-started, empty local cache. Later calls
    /// read rows at or after <c>highWater - margin</c>; the <c>&gt;=</c> overlap plus margin covers a
    /// long transaction whose update time precedes its commit visibility, and the version comparison
    /// keeps the overlap idempotent (a key absent from the mirror counts as version 0, so a key first
    /// seen after startup evicts once — a no-op when nothing is cached).
    /// </para>
    /// <para>
    /// The high-water threshold is passed as an ISO-8601 <b>string</b> and cast to the column type in
    /// SQL (<c>CAST</c> / <c>TO_TIMESTAMP</c>), deliberately avoiding ADO.NET <c>DateTime</c>
    /// parameter binding: providers map <c>DbType.DateTime</c> inconsistently (e.g. Npgsql resolves it
    /// to <c>timestamptz</c> and rejects a non-UTC <c>Kind</c>), which does not match the tz-naive
    /// <c>sys_update_time</c> column.
    /// </para>
    /// </remarks>
    public sealed class CacheNotifyPollSession
    {
        private const string TableName = "st_cache_notify";
        private const string KeyColumn = "sys_cache_key";
        private const string VersionColumn = "sys_cache_version";
        private const string UpdateTimeColumn = "sys_update_time";

        private readonly IDbAccessFactory _dbAccessFactory;
        private readonly string _databaseId;
        private readonly ICacheContainer _container;
        private readonly ICacheNotifyRouter _router;
        private readonly TimeSpan _margin;

        private readonly Dictionary<string, long> _mirror = new(StringComparer.Ordinal);
        private DateTime _highWater;
        private bool _baselineTaken;

        /// <summary>
        /// Initializes a new <see cref="CacheNotifyPollSession"/>.
        /// </summary>
        /// <param name="databaseId">The database whose <c>st_cache_notify</c> table is polled.</param>
        /// <param name="dbAccessFactory">Factory for the database access object.</param>
        /// <param name="container">The cache container eviction actions operate on.</param>
        /// <param name="router">The route table mapping cache groups to eviction actions.</param>
        /// <param name="marginSeconds">The overlap safety margin in seconds.</param>
        public CacheNotifyPollSession(
            string databaseId,
            IDbAccessFactory dbAccessFactory,
            ICacheContainer container,
            ICacheNotifyRouter router,
            int marginSeconds)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);
            ArgumentNullException.ThrowIfNull(dbAccessFactory);
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(router);

            _databaseId = databaseId;
            _dbAccessFactory = dbAccessFactory;
            _container = container;
            _router = router;
            _margin = TimeSpan.FromSeconds(marginSeconds < 0 ? 0 : marginSeconds);
        }

        /// <summary>
        /// Performs one polling cycle. The first call only takes the baseline cursor; later calls
        /// fetch the incremental delta and route evictions for keys whose version advanced.
        /// </summary>
        public void Poll()
        {
            var dbAccess = _dbAccessFactory.Create(_databaseId);
            var databaseType = dbAccess.DatabaseType;

            if (!_baselineTaken)
            {
                _highWater = ReadBaseline(dbAccess, databaseType);
                _baselineTaken = true;
                return;
            }

            PollDelta(dbAccess, databaseType);
        }

        // Baseline cursor = max(update_time); an empty table falls back to the server's current time
        // so we track only post-startup bumps rather than replaying history. Read as a naive value
        // (no parameters involved) to match the tz-naive column.
        private static DateTime ReadBaseline(DbAccess dbAccess, DatabaseType databaseType)
        {
            string tbl = databaseType.QuoteIdentifier(TableName);
            string upd = databaseType.QuoteIdentifier(UpdateTimeColumn);

            var scalar = dbAccess.ExecuteScalar($"SELECT MAX({upd}) FROM {tbl}");
            if (scalar is null || scalar is DBNull)
                scalar = dbAccess.ExecuteScalar(NaiveNowCommandText(databaseType));

            return Convert.ToDateTime(scalar, CultureInfo.InvariantCulture);
        }

        private void PollDelta(DbAccess dbAccess, DatabaseType databaseType)
        {
            string tbl = databaseType.QuoteIdentifier(TableName);
            string key = databaseType.QuoteIdentifier(KeyColumn);
            string ver = databaseType.QuoteIdentifier(VersionColumn);
            string upd = databaseType.QuoteIdentifier(UpdateTimeColumn);

            var (format, castTemplate) = ThresholdBinding(databaseType);
            string threshold = (_highWater - _margin).ToString(format, CultureInfo.InvariantCulture);

            // castTemplate carries the literal {0} placeholder DbCommandSpec resolves to the param.
            var table = dbAccess.ExecuteDataTable(
                $"SELECT {key}, {ver}, {upd} FROM {tbl} WHERE {upd} >= {castTemplate}", threshold);
            if (table == null) return;

            DateTime maxSeen = _highWater;
            foreach (System.Data.DataRow row in table.Rows)
            {
                string cacheKey = Convert.ToString(row[0], CultureInfo.InvariantCulture) ?? string.Empty;
                if (cacheKey.Length == 0) continue;

                long version = Convert.ToInt64(row[1], CultureInfo.InvariantCulture);
                DateTime updateTime = Convert.ToDateTime(row[2], CultureInfo.InvariantCulture);

                // Idempotent across the overlap window: evict only on a strictly higher version.
                _mirror.TryGetValue(cacheKey, out long mirrored);
                if (version > mirrored)
                {
                    _router.TryInvoke(_container, cacheKey);
                    _mirror[cacheKey] = version;
                }

                if (updateTime > maxSeen) maxSeen = updateTime;
            }

            _highWater = maxSeen;
        }

        // Server "now" as a tz-naive value matching the sys_update_time column type. Distinct from
        // the column's default expression, which is tz-aware on PostgreSQL/Oracle.
        private static string NaiveNowCommandText(DatabaseType databaseType) => databaseType switch
        {
            DatabaseType.SQLServer => "SELECT getdate()",
            DatabaseType.PostgreSQL => "SELECT LOCALTIMESTAMP",
            DatabaseType.MySQL => "SELECT CURRENT_TIMESTAMP(6)",
            DatabaseType.Oracle => "SELECT LOCALTIMESTAMP FROM dual",
            DatabaseType.SQLite => "SELECT CURRENT_TIMESTAMP",
            _ => throw new NotSupportedException($"Cache-notify baseline now is not defined for {databaseType}.")
        };

        // (DateTime format, SQL cast template) for the high-water threshold passed as a string.
        // Formats are paired with their cast so MySQL (space separator) and the ISO-8601 'T'
        // dialects each parse unambiguously and locale-independently. SQLite stores text without
        // fractional seconds, so it compares the value lexically with a matching format.
        private static (string Format, string CastTemplate) ThresholdBinding(DatabaseType databaseType) => databaseType switch
        {
            DatabaseType.SQLServer => ("yyyy-MM-ddTHH:mm:ss.fffffff", "CAST({0} AS datetime2)"),
            DatabaseType.PostgreSQL => ("yyyy-MM-ddTHH:mm:ss.ffffff", "CAST({0} AS timestamp)"),
            DatabaseType.MySQL => ("yyyy-MM-dd HH:mm:ss.ffffff", "CAST({0} AS DATETIME(6))"),
            DatabaseType.Oracle => ("yyyy-MM-ddTHH:mm:ss.ffffff", "TO_TIMESTAMP({0}, 'YYYY-MM-DD\"T\"HH24:MI:SS.FF6')"),
            DatabaseType.SQLite => ("yyyy-MM-dd HH:mm:ss", "{0}"),
            _ => throw new NotSupportedException($"Cache-notify threshold binding is not defined for {databaseType}.")
        };
    }
}
