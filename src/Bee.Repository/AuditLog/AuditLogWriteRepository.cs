using System.Text;
using Bee.Definition;
using Bee.Db;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Repository.Abstractions.AuditLog;

namespace Bee.Repository.AuditLog
{
    /// <summary>
    /// Default <see cref="IAuditLogWriteRepository"/>. Writes parameterised INSERTs into the
    /// conventional log database via <see cref="IDbAccessFactory"/>. Table and column names match
    /// the read side (<see cref="AuditLogRepository"/>) so reads line up with writes across every
    /// provider.
    /// </summary>
    public class AuditLogWriteRepository : RepositoryBase, IAuditLogWriteRepository
    {
        /// <summary>
        /// Initializes a new <see cref="AuditLogWriteRepository"/>.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">Unused on the framework axis; accepted for signature uniformity.</param>
        public AuditLogWriteRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : base(ctx, accessToken, progId, DbScope.Log)
        {
        }

        /// <inheritdoc/>
        public void WriteBatch(IReadOnlyList<AuditEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            if (entries.Count == 0) { return; }

            // Log tables live in the conventional 'log' database (a fixed databaseId, like
            // 'common'); the physical mapping is resolved by DatabaseSettings, not configured here.
            var dbAccess = Context.DbAccessFactory.Create(DbCategoryIds.Log);
            foreach (var entry in entries)
            {
                dbAccess.Execute(BuildInsert(entry));
            }
        }

        /// <summary>
        /// Builds a parameterised INSERT for one entry. Column order is the entry's own stable
        /// order; values bind positionally through <c>{@Parameters}</c>, with null mapped to
        /// <see cref="DBNull.Value"/> so nullable columns are written as SQL NULL.
        /// </summary>
        /// <remarks>
        /// Public because it is the canonical write-side column contract: tests that seed audit
        /// rows for read-side queries build them through this method rather than restating the
        /// column list, so the two sides cannot drift apart.
        /// </remarks>
        /// <param name="entry">The entry to persist.</param>
        public static DbCommandSpec BuildInsert(AuditEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

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
    }
}
