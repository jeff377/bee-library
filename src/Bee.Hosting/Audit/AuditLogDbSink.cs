using System.Data.Common;
using System.Text;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.AuditLog;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.Audit
{
    /// <summary>
    /// Persists <see cref="AuditEntry"/> rows through <see cref="IAuditLogWriteRepository"/>.
    /// A failed write is logged and, when a fallback path is configured, spilled to a file so audit
    /// entries survive a transient log-database outage. This is the terminal sink shared by both the
    /// background and synchronous writers.
    /// </summary>
    /// <remarks>
    /// Holds no SQL: statement construction and execution live in the repository layer. What stays
    /// here is the hosting-layer concern — deciding what happens when the write fails.
    /// </remarks>
    internal sealed class AuditLogDbSink : IAuditLogSink
    {
        private readonly IAuditLogWriteRepository _repository;
        private readonly AuditLogOptions _options;
        private readonly ILogger<AuditLogDbSink> _logger;

        /// <summary>
        /// Initializes a new <see cref="AuditLogDbSink"/>.
        /// </summary>
        public AuditLogDbSink(IAuditLogWriteRepository repository, AuditLogOptions options, ILogger<AuditLogDbSink> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
                _repository.WriteBatch(entries);
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
