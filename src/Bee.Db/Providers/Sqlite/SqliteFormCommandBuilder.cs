using System.Data;
using Bee.Db.Dml;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Database;
using Bee.Definition.Storage;
using Bee.Definition.Sorting;

namespace Bee.Db.Providers.Sqlite
{
    /// <summary>
    /// SQLite form-related SQL command builder, generating Select, Insert, Update, and Delete
    /// statements. Counterpart to <see cref="SqlServer.SqlFormCommandBuilder"/> and
    /// <see cref="PostgreSql.PgFormCommandBuilder"/> for the SQLite provider; all four methods
    /// delegate to the dialect-agnostic cores in <see cref="Bee.Db.Dml"/>.
    /// </summary>
    public class SqliteFormCommandBuilder : IFormCommandBuilder
    {
        private readonly IDefineAccess _defineAccess;

        /// <summary>
        /// Initializes a new instance of <see cref="SqliteFormCommandBuilder"/> using the specified form schema.
        /// </summary>
        /// <param name="formDefine">The form schema definition.</param>
        /// <param name="defineAccess">The define access service used to resolve relation-form schemas during SELECT construction.</param>
        public SqliteFormCommandBuilder(FormSchema formDefine, IDefineAccess defineAccess)
        {
            FormSchema = formDefine ?? throw new ArgumentNullException(nameof(formDefine));
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        }

        /// <summary>
        /// Gets the form schema definition.
        /// </summary>
        private FormSchema FormSchema { get; }

        /// <summary>
        /// Builds the SELECT command specification.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="selectFields">A comma-separated list of field names; empty string retrieves all fields.</param>
        /// <param name="filter">The filter condition.</param>
        /// <param name="sortFields">The sort field collection.</param>
        public DbCommandSpec BuildSelect(string tableName, string selectFields, FilterNode? filter = null, SortFieldCollection? sortFields = null)
        {
            var builder = new SelectCommandBuilder(FormSchema, DatabaseType.SQLite, _defineAccess);
            return builder.Build(tableName, selectFields, filter, sortFields);
        }

        /// <summary>
        /// Builds the INSERT command specification.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="row">The data row to insert.</param>
        public DbCommandSpec BuildInsert(string tableName, DataRow row)
        {
            var builder = new InsertCommandBuilder(FormSchema, DatabaseType.SQLite);
            return builder.Build(tableName, row);
        }

        /// <summary>
        /// Builds the UPDATE command specification.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="row">The modified data row.</param>
        public DbCommandSpec BuildUpdate(string tableName, DataRow row)
        {
            var builder = new UpdateCommandBuilder(FormSchema, DatabaseType.SQLite);
            return builder.Build(tableName, row);
        }

        /// <summary>
        /// Builds the DELETE command specification.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="filter">The filter that becomes the WHERE clause; must not be null.</param>
        public DbCommandSpec BuildDelete(string tableName, FilterNode filter)
        {
            var builder = new DeleteCommandBuilder(FormSchema, DatabaseType.SQLite);
            return builder.Build(tableName, filter);
        }
    }
}
