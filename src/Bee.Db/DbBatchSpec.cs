using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// Describes a batch of database commands to execute.
    /// </summary>
    public class DbBatchSpec
    {
        /// <summary>
        /// Gets or sets the collection of commands to execute (executed in order).
        /// </summary>
        public DbCommandSpecCollection Commands { get; set; } = new DbCommandSpecCollection();

        /// <summary>
        /// Gets or sets whether to wrap the entire batch in a transaction (rolls back on any failure; commits on success).
        /// </summary>
        public bool UseTransaction { get; set; } = false;

        /// <summary>
        /// Gets or sets the transaction isolation level (applicable when <see cref="UseTransaction"/> is <c>true</c>).
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }
    }
}
