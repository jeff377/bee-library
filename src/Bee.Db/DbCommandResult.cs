using Bee.Base;
using Bee.Base.Collections;
using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// A unified wrapper for a DbCommand execution result.
    /// </summary>
    public class DbCommandResult : CollectionItem
    {
        /// <summary>
        /// Gets the execution kind of the database command.
        /// </summary>
        public DbCommandKind Kind { get; private set; }

        /// <summary>
        /// Gets the number of rows affected (NonQuery).
        /// </summary>
        public int RowsAffected { get; private set; }

        /// <summary>
        /// Gets the scalar result value (Scalar).
        /// </summary>
        public object Scalar { get; private set; }

        /// <summary>
        /// Gets the result DataTable (DataTable).
        /// </summary>
        public DataTable Table { get; private set; }

        /// <summary>
        /// Creates a NonQuery result wrapper.
        /// </summary>
        /// <param name="rows">The number of rows affected.</param>
        public static DbCommandResult ForRowsAffected(int rows)
            => new DbCommandResult { Kind = DbCommandKind.NonQuery, RowsAffected = rows };

        /// <summary>
        /// Creates a Scalar result wrapper.
        /// </summary>
        /// <param name="value">The scalar result value.</param>
        public static DbCommandResult ForScalar(object value)
            => new DbCommandResult { Kind = DbCommandKind.Scalar, Scalar = value };

        /// <summary>
        /// Creates a DataTable result wrapper.
        /// </summary>
        /// <param name="table">The result DataTable.</param>
        public static DbCommandResult ForTable(DataTable table)
            => new DbCommandResult { Kind = DbCommandKind.DataTable, Table = table };
    }
}
