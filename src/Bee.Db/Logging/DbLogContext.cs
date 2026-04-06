using System;
using System.Diagnostics;

namespace Bee.Db.Logging
{
    /// <summary>
    /// Context information for a database access log entry.
    /// Records the command text, database identifier, execution timer, and start time.
    /// </summary>
    public sealed class DbLogContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DbLogContext"/>.
        /// </summary>
        /// <param name="commandText">The SQL command text.</param>
        /// <param name="databaseId">The database identifier.</param>
        internal DbLogContext(string commandText, string databaseId)
        {
            CommandText = commandText ?? string.Empty;
            DatabaseId = databaseId ?? string.Empty;
            Stopwatch = Stopwatch.StartNew();
            StartedAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the SQL command text.
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// Gets the database identifier.
        /// </summary>
        public string DatabaseId { get; }

        /// <summary>
        /// Gets the stopwatch that records the elapsed time of command execution.
        /// </summary>
        public Stopwatch Stopwatch { get; }

        /// <summary>
        /// Gets the UTC timestamp at which the command started executing.
        /// </summary>
        public DateTime StartedAtUtc { get; }
    }
}
