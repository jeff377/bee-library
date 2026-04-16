using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// Carries a DataTable together with its three command specifications required for a DataTable update operation.
    /// </summary>
    public sealed class DataTableUpdateSpec
    {
        /// <summary>
        /// Gets or sets the DataTable to write back to the database.
        /// </summary>
        public DataTable DataTable { get; set; }

        /// <summary>
        /// Gets or sets the INSERT command specification.
        /// </summary>
        public DbCommandSpec InsertCommand { get; set; }

        /// <summary>
        /// Gets or sets the UPDATE command specification.
        /// </summary>
        public DbCommandSpec UpdateCommand { get; set; }

        /// <summary>
        /// Gets or sets the DELETE command specification.
        /// </summary>
        public DbCommandSpec DeleteCommand { get; set; }

        /// <summary>
        /// Gets or sets whether to wrap the entire update in a transaction (rolls back on any failure; commits on success).
        /// </summary>
        public bool UseTransaction { get; set; } = false;

        /// <summary>
        /// Gets or sets the transaction isolation level (applicable when <see cref="UseTransaction"/> is <c>true</c>).
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }
    }
}
