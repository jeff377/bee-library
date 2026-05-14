using System.Data;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Sorting;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Form
{
    /// <summary>
    /// Repository implementation for data forms; executes FormSchema-driven SELECT
    /// statements against the master table via the dialect-specific
    /// <c>IFormCommandBuilder</c>.
    /// </summary>
    public class DataFormRepository : IDataFormRepository
    {
        private readonly FormSchema _schema;
        private readonly IDefineAccess _defineAccess;
        private readonly IDbAccessFactory _dbAccessFactory;
        private readonly IDbConnectionManager _connectionManager;
        private readonly string _databaseId;

        /// <summary>
        /// Initializes a new instance of <see cref="DataFormRepository"/>.
        /// </summary>
        /// <param name="progId">The program identifier (also the master table name).</param>
        /// <param name="schema">The resolved form schema for <paramref name="progId"/>.</param>
        /// <param name="defineAccess">
        /// The define access service, forwarded to <c>IFormCommandBuilder</c> to resolve
        /// relation-form schemas during SELECT construction.
        /// </param>
        /// <param name="dbAccessFactory">The DI-resolved database access factory.</param>
        /// <param name="connectionManager">
        /// The DI-resolved connection manager, used to look up the database type for
        /// dialect routing.
        /// </param>
        /// <param name="databaseId">The database identifier used for connection and dialect resolution.</param>
        public DataFormRepository(
            string progId,
            FormSchema schema,
            IDefineAccess defineAccess,
            IDbAccessFactory dbAccessFactory,
            IDbConnectionManager connectionManager,
            string databaseId)
        {
            ProgId = progId ?? throw new ArgumentNullException(nameof(progId));
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _dbAccessFactory = dbAccessFactory ?? throw new ArgumentNullException(nameof(dbAccessFactory));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _databaseId = databaseId ?? throw new ArgumentNullException(nameof(databaseId));
        }

        /// <summary>
        /// Gets the program identifier.
        /// </summary>
        public string ProgId { get; }

        /// <inheritdoc/>
        public DataTable? GetList(
            string selectFields,
            FilterNode? filter,
            SortFieldCollection? sortFields)
        {
            // FormSchema.MasterTable.TableName == ProgId (framework invariant), so we
            // pass ProgId directly as the target table name.
            var resolvedSelectFields = StringUtilities.IsNotEmpty(selectFields)
                ? selectFields
                : _schema.ListFields;  // empty falls through to SelectCommandBuilder as "all fields"

            var connInfo = _connectionManager.GetConnectionInfo(_databaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, _defineAccess);
            var spec = builder.BuildSelect(ProgId, resolvedSelectFields, filter, sortFields);

            var dbAccess = _dbAccessFactory.Create(_databaseId);
            return dbAccess.Execute(spec).Table;
        }
    }
}
