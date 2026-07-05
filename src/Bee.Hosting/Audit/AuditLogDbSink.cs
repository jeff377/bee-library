using System.Data.Common;
using System.Text;
using Bee.Db;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.Audit
{
    /// <summary>
    /// Persists <see cref="AuditEntry"/> rows into the log database via <see cref="IDbAccessFactory"/>.
    /// A failed write is logged and, when a fallback path is configured, spilled to a file so audit
    /// entries survive a transient log-database outage. This is the terminal sink shared by both the
    /// background and synchronous writers.
    /// </summary>
    internal sealed class AuditLogDbSink : IAuditLogSink
    {
        private readonly IDbAccessFactory _dbAccessFactory;
        private readonly AuditLogOptions _options;
        private readonly ILogger<AuditLogDbSink> _logger;

        /// <summary>
        /// Initializes a new <see cref="AuditLogDbSink"/>.
        /// </summary>
        public AuditLogDbSink(IDbAccessFactory dbAccessFactory, AuditLogOptions options, ILogger<AuditLogDbSink> logger)
        {
            _dbAccessFactory = dbAccessFactory ?? throw new ArgumentNullException(nameof(dbAccessFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public void WriteBatch(IReadOnlyList<AuditEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            if (entries.Count == 0) { return; }

            try
            {
                // Log tables live in the conventional 'log' database (a fixed databaseId, like
                // 'common'); the physical mapping is resolved by DatabaseSettings, not configured here.
                var dbAccess = _dbAccessFactory.Create(DbCategoryIds.Log);
                foreach (var entry in entries)
                {
                    dbAccess.Execute(BuildInsert(entry));
                }
            }
            catch (DbException ex)
            {
                // Resilience: a log-store outage must not surface into the business flow. DbException
                // covers every provider's exception type (Sql / Npgsql / MySql / Oracle).
                _logger.LogError(ex, "Audit log write failed against the '{DatabaseId}' database.", DbCategoryIds.Log);
                SpillToFile(entries);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Audit log write failed against the '{DatabaseId}' database.", DbCategoryIds.Log);
                SpillToFile(entries);
            }
        }

        /// <summary>
        /// Builds a parameterised INSERT for one entry. Column order is the entry's own stable
        /// order; values bind positionally through <c>{@Parameters}</c>, with null mapped to
        /// <see cref="DBNull.Value"/> so nullable columns are written as SQL NULL.
        /// </summary>
        private static DbCommandSpec BuildInsert(AuditEntry entry)
        {
            var columns = entry.GetColumns();

            var sb = new StringBuilder();
            sb.Append("INSERT INTO ").Append(entry.TableName).Append(" (");
            for (int i = 0; i < columns.Count; i++)
            {
                if (i > 0) { sb.Append(", "); }
                sb.Append(columns[i].Name);
            }
            sb.Append(") VALUES (").Append(CommandTextVariable.Parameters).Append(')');

            var values = new object[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                values[i] = columns[i].Value ?? DBNull.Value;
            }

            return new DbCommandSpec(DbCommandKind.NonQuery, sb.ToString(), values);
        }

        /// <summary>
        /// Appends entries to the configured fallback file (one tab-delimited line per entry) so a
        /// database outage does not lose audit records. No-op when no fallback path is configured.
        /// </summary>
        private void SpillToFile(IReadOnlyList<AuditEntry> entries)
        {
            var path = _options.FileFallbackPath;
            if (string.IsNullOrEmpty(path)) { return; }

            try
            {
                var sb = new StringBuilder();
                foreach (var entry in entries)
                {
                    sb.Append(entry.TableName);
                    foreach (var column in entry.GetColumns())
                    {
                        sb.Append('\t').Append(column.Name).Append('=').Append(column.Value);
                    }
                    sb.AppendLine();
                }
                File.AppendAllText(path, sb.ToString());
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Audit log file fallback write failed at '{Path}'.", path);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Audit log file fallback write failed at '{Path}'.", path);
            }
        }
    }
}
