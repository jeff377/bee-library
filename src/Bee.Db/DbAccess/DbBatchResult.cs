using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Db.DbAccess
{
    /// <summary>
    /// Represents the output of a batch command execution.
    /// </summary>
    public class DbBatchResult
    {
        /// <summary>
        /// Gets or sets the results for each command in the batch (in input order).
        /// </summary>
        public DbCommandResultCollection Results { get; set; } = new DbCommandResultCollection();

        /// <summary>
        /// Gets or sets the total number of rows affected (accumulated only for NonQuery commands).
        /// </summary>
        public int RowsAffectedSum { get; set; }
    }
}
