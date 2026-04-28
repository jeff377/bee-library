using System.Data;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Sorting;

namespace Bee.Db.Providers.MySql
{
    /// <summary>
    /// MySQL 8.0+ form-related SQL command builder, generating SELECT, INSERT, UPDATE,
    /// and DELETE statements. Counterpart to <see cref="Sqlite.SqliteFormCommandBuilder"/>
    /// and <see cref="PostgreSql.PgFormCommandBuilder"/>; all four methods delegate to
    /// the dialect-agnostic cores in <see cref="Bee.Db.Dml"/> with
    /// <see cref="DatabaseType.MySQL"/>, so backtick identifier quoting and other
    /// MySQL-specific output flow from the <see cref="DbFunc"/> dictionaries.
    /// </summary>
    public class MySqlFormCommandBuilder : IFormCommandBuilder
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of <see cref="MySqlFormCommandBuilder"/> using the specified program ID.
        /// </summary>
        /// <param name="progId">The form program identifier.</param>
        public MySqlFormCommandBuilder(string progId)
        {
            FormSchema = BackendInfo.DefineAccess.GetFormSchema(progId);
            if (FormSchema == null)
                throw new ArgumentException($"Form definition not found for program ID '{progId}'.", nameof(progId));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MySqlFormCommandBuilder"/> using the specified form schema.
        /// </summary>
        /// <param name="formDefine">The form schema definition.</param>
        public MySqlFormCommandBuilder(FormSchema formDefine)
        {
            FormSchema = formDefine ?? throw new ArgumentNullException(nameof(formDefine));
        }

        #endregion

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
            var builder = new SelectCommandBuilder(FormSchema, DatabaseType.MySQL);
            return builder.Build(tableName, selectFields, filter, sortFields);
        }

        /// <summary>
        /// Builds the INSERT command specification.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="row">The data row to insert.</param>
        public DbCommandSpec BuildInsert(string tableName, DataRow row)
        {
            var builder = new InsertCommandBuilder(FormSchema, DatabaseType.MySQL);
            return builder.Build(tableName, row);
        }

        /// <summary>
        /// Builds the UPDATE command specification.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="row">The modified data row.</param>
        public DbCommandSpec BuildUpdate(string tableName, DataRow row)
        {
            var builder = new UpdateCommandBuilder(FormSchema, DatabaseType.MySQL);
            return builder.Build(tableName, row);
        }

        /// <summary>
        /// Builds the DELETE command specification.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="filter">The filter that becomes the WHERE clause; must not be null.</param>
        public DbCommandSpec BuildDelete(string tableName, FilterNode filter)
        {
            var builder = new DeleteCommandBuilder(FormSchema, DatabaseType.MySQL);
            return builder.Build(tableName, filter);
        }
    }
}
