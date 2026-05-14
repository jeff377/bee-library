using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Form;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default implementation of <see cref="IFormRepositoryFactory"/>; resolves the
    /// per-form schema and binds the dialect-aware <see cref="DataFormRepository"/>
    /// to the appropriate database connection.
    /// </summary>
    public class FormRepositoryFactory : IFormRepositoryFactory
    {
        private readonly IDefineAccess _defineAccess;
        private readonly IDbAccessFactory _dbAccessFactory;
        private readonly IDbConnectionManager _connectionManager;

        /// <summary>
        /// Initializes a new instance of <see cref="FormRepositoryFactory"/>.
        /// </summary>
        /// <param name="defineAccess">The DI-resolved define access service.</param>
        /// <param name="dbAccessFactory">The DI-resolved database access factory.</param>
        /// <param name="connectionManager">The DI-resolved connection manager (for dialect routing).</param>
        public FormRepositoryFactory(
            IDefineAccess defineAccess,
            IDbAccessFactory dbAccessFactory,
            IDbConnectionManager connectionManager)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _dbAccessFactory = dbAccessFactory ?? throw new ArgumentNullException(nameof(dbAccessFactory));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        /// <inheritdoc/>
        public IDataFormRepository CreateDataFormRepository(string progId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);
            var schema = _defineAccess.GetFormSchema(progId);
            if (StringUtilities.IsEmpty(schema.CategoryId))
                throw new InvalidOperationException(
                    $"FormSchema '{progId}' does not specify a CategoryId; cannot resolve target database.");

            // CategoryId is used directly as databaseId. The framework enforces
            // Id == CategoryId == "common" for the common category; multi-tenant
            // deployments override this factory to map company/log categories to
            // the per-tenant physical DatabaseItem.Id.
            var databaseId = schema.CategoryId;

            return new DataFormRepository(
                progId,
                schema,
                _defineAccess,
                _dbAccessFactory,
                _connectionManager,
                databaseId);
        }

        /// <inheritdoc/>
        public IReportFormRepository CreateReportFormRepository(string progId)
        {
            return new ReportFormRepository(progId);
        }
    }
}
