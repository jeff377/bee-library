using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Form;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default implementation of <see cref="IFormRepositoryFactory"/>; resolves the
    /// per-form schema and binds the dialect-aware <see cref="DataFormRepository"/>
    /// to the appropriate database connection via <see cref="IRepositoryDatabaseRouter"/>.
    /// </summary>
    public class FormRepositoryFactory : IFormRepositoryFactory
    {
        private readonly IDefineAccess _defineAccess;
        private readonly IDbAccessFactory _dbAccessFactory;
        private readonly IDbConnectionManager _connectionManager;
        private readonly IRepositoryDatabaseRouter _router;

        /// <summary>
        /// Initializes a new instance of <see cref="FormRepositoryFactory"/>.
        /// </summary>
        /// <param name="defineAccess">The DI-resolved define access service.</param>
        /// <param name="dbAccessFactory">The DI-resolved database access factory.</param>
        /// <param name="connectionManager">The DI-resolved connection manager (for dialect routing).</param>
        /// <param name="router">The DI-resolved repository database router (resolves logical scope to physical databaseId).</param>
        public FormRepositoryFactory(
            IDefineAccess defineAccess,
            IDbAccessFactory dbAccessFactory,
            IDbConnectionManager connectionManager,
            IRepositoryDatabaseRouter router)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _dbAccessFactory = dbAccessFactory ?? throw new ArgumentNullException(nameof(dbAccessFactory));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        /// <inheritdoc/>
        public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);
            var schema = _defineAccess.GetFormSchema(progId);
            if (StringUtilities.IsEmpty(schema.CategoryId))
                throw new InvalidOperationException(
                    $"FormSchema '{progId}' does not specify a CategoryId; cannot resolve target database.");

            var scope = ParseCategoryId(schema.CategoryId);
            var databaseId = _router.Resolve(scope, accessToken);

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

        private static DbScope ParseCategoryId(string categoryId)
            => categoryId switch
            {
                DbCategoryIds.Common  => DbScope.Common,
                DbCategoryIds.Company => DbScope.Company,
                DbCategoryIds.Log     => DbScope.Log,
                _ => throw new InvalidOperationException(
                    $"Unknown schema.CategoryId '{categoryId}'.")
            };
    }
}
