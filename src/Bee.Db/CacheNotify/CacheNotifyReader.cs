using System.Data;
using System.Globalization;
using Bee.Definition.Database;

namespace Bee.Db.CacheNotify
{
    /// <summary>
    /// Default <see cref="ICacheNotifyReader"/>. Issues the dialect-appropriate SELECT against
    /// <c>st_cache_notify</c> through <see cref="IDbAccessFactory"/>.
    /// </summary>
    /// <remarks>
    /// Stateless: the poller's cursor and mirror live in the caller, so one instance serves every
    /// database id and every poll loop.
    /// <para>
    /// The high-water threshold is passed as an ISO-8601 <b>string</b> and cast to the column type in
    /// SQL (<c>CAST</c> / <c>TO_TIMESTAMP</c>), deliberately avoiding ADO.NET <c>DateTime</c>
    /// parameter binding: providers map <c>DbType.DateTime</c> inconsistently (e.g. Npgsql resolves it
    /// to <c>timestamptz</c> and rejects a non-UTC <c>Kind</c>), which does not match the tz-naive
    /// <c>sys_update_time</c> column.
    /// </para>
    /// </remarks>
    public sealed class CacheNotifyReader : ICacheNotifyReader
    {
        private const string TableName = "st_cache_notify";
        private const string KeyColumn = "cache_key";
        private const string VersionColumn = "cache_version";
        private const string UpdateTimeColumn = "sys_update_time";

        private readonly IDbAccessFactory _dbAccessFactory;

        /// <summary>
        /// Initializes a new <see cref="CacheNotifyReader"/>.
        /// </summary>
        /// <param name="dbAccessFactory">Factory for the database access object.</param>
        public CacheNotifyReader(IDbAccessFactory dbAccessFactory)
        {
            ArgumentNullException.ThrowIfNull(dbAccessFactory);
            _dbAccessFactory = dbAccessFactory;
        }

        /// <inheritdoc/>
        public DateTime ReadBaseline(string databaseId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbAccess = _dbAccessFactory.Create(databaseId);
            var databaseType = dbAccess.DatabaseType;

            string tbl = databaseType.QuoteIdentifier(TableName);
            string upd = databaseType.QuoteIdentifier(UpdateTimeColumn);

            // Read as a naive value (no parameters involved) to match the tz-naive column.
            var scalar = dbAccess.ExecuteScalar($"SELECT MAX({upd}) FROM {tbl}");
            if (scalar is null || scalar is DBNull)
                scalar = dbAccess.ExecuteScalar(NaiveNowCommandText(databaseType));

            return Convert.ToDateTime(scalar, CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public IReadOnlyList<CacheNotifyChange> ReadChangesSince(string databaseId, DateTime threshold)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseId);

            var dbAccess = _dbAccessFactory.Create(databaseId);
            var databaseType = dbAccess.DatabaseType;

            string tbl = databaseType.QuoteIdentifier(TableName);
            string key = databaseType.QuoteIdentifier(KeyColumn);
            string ver = databaseType.QuoteIdentifier(VersionColumn);
            string upd = databaseType.QuoteIdentifier(UpdateTimeColumn);

            var (format, castTemplate) = ThresholdBinding(databaseType);
            string thresholdText = threshold.ToString(format, CultureInfo.InvariantCulture);

            // castTemplate carries the literal {0} placeholder DbCommandSpec resolves to the param.
            var table = dbAccess.ExecuteDataTable(
                $"SELECT {key}, {ver}, {upd} FROM {tbl} WHERE {upd} >= {castTemplate}", thresholdText);
            if (table == null) return Array.Empty<CacheNotifyChange>();

            var changes = new List<CacheNotifyChange>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                string cacheKey = Convert.ToString(row[0], CultureInfo.InvariantCulture) ?? string.Empty;
                if (cacheKey.Length == 0) continue;

                changes.Add(new CacheNotifyChange(
                    cacheKey,
                    Convert.ToInt64(row[1], CultureInfo.InvariantCulture),
                    Convert.ToDateTime(row[2], CultureInfo.InvariantCulture)));
            }
            return changes;
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
