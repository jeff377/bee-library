using System.Data;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition;
using Bee.Db.Sql;

namespace Bee.Db.Providers.SqlServer
{
    /// <summary>
    /// SQL Server form-related SQL command builder, generating Select, Insert, Update, and Delete statements.
    /// </summary>
    public class SqlFormCommandBuilder : IFormCommandBuilder
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of <see cref="SqlFormCommandBuilder"/> using the specified program ID.
        /// </summary>
        /// <param name="progID">The program identifier.</param>
        public SqlFormCommandBuilder(string progID)
        {
            FormSchema = BackendInfo.DefineAccess.GetFormSchema(progID);
            if (FormSchema == null)
                throw new ArgumentException($"Form definition not found for program ID '{progID}'.", nameof(progID));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlFormCommandBuilder"/> using the specified form schema.
        /// </summary>
        public SqlFormCommandBuilder(FormSchema formDefine)
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
            var builder = new SelectCommandBuilder(FormSchema, DatabaseType.SQLServer);
            return builder.Build(tableName, selectFields, filter, sortFields);
        }

        /// <summary>
        /// Builds the INSERT command specification.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="row">The data row to insert.</param>
        public DbCommandSpec BuildInsert(string tableName, DataRow row)
        {
            var builder = new InsertCommandBuilder(FormSchema, DatabaseType.SQLServer);
            return builder.Build(tableName, row);
        }

        /// <summary>
        /// Builds the UPDATE command specification.
        /// </summary>
        public DbCommandSpec BuildUpdate()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Builds the DELETE command specification.
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="filter">The filter that becomes the WHERE clause; must not be null.</param>
        public DbCommandSpec BuildDelete(string tableName, FilterNode filter)
        {
            var builder = new DeleteCommandBuilder(FormSchema, DatabaseType.SQLServer);
            return builder.Build(tableName, filter);
        }
    }
}
