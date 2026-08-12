using Bee.Business.Form;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Form;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// Per-test wiring shared by the new <c>FormBusinessObject</c> CRUD tests
    /// (<c>GetNewData</c> / <c>GetData</c> / <c>Save</c> / <c>Delete</c>).
    /// Binds the BO to a <see cref="DataFormRepository"/> constructed against
    /// the test-specific <c>{categoryId}_{dbtype}</c> databaseId, mirroring
    /// the GetList tests' pattern.
    /// </summary>
    internal sealed class CrudTestContext
    {
        public const string CategoryId = "company";
        public const string ProgId = "Employee";

        private readonly SharedDbFixture _fx;
        private readonly string _databaseId;
        private readonly IDataFormRepository _repository;

        public CrudTestContext(SharedDbFixture fx, DatabaseType dbType)
        {
            _fx = fx;
            DbType = dbType;
            _databaseId = TestDbConventions.GetDatabaseId(dbType, CategoryId);
            DbAccess = fx.NewDbAccess(_databaseId);

            var defineAccess = fx.GetRequiredService<IDefineAccess>();
            EmployeeSchema = defineAccess.GetFormSchema(ProgId);

            _repository = new DataFormRepository(TestRepositoryContext.Create(fx.GetRequiredService<IDbConnectionManager>(), defineAccess: defineAccess, dbAccessFactory: fx.GetRequiredService<IDbAccessFactory>()), ProgId, EmployeeSchema, _databaseId);
        }

        public DatabaseType DbType { get; }
        public DbAccess DbAccess { get; }
        public FormSchema EmployeeSchema { get; }
        public IDataFormRepository Repository => _repository;

        /// <summary>
        /// Builds a business object bound to the test repository.
        /// </summary>
        /// <param name="pluginResolver">
        /// Optional plugin chain resolver. Supplied by the plugin integration tests to bind a
        /// chain without writing a customization definition file; omitted elsewhere, in which case
        /// the fixture's own resolver applies and no plugin is bound.
        /// </param>
        public FormBusinessObject CreateBo(IFormPluginResolver? pluginResolver = null)
        {
            var factory = new StubFactory(_repository);
            var ctx = pluginResolver == null
                ? TestBeeContext.CreateWithOverrides(_fx, (typeof(IRepositoryFactory), factory))
                : TestBeeContext.CreateWithOverrides(_fx,
                    (typeof(IRepositoryFactory), factory),
                    (typeof(IFormPluginResolver), pluginResolver));
            return new FormBusinessObject(ctx, Guid.NewGuid(), ProgId);
        }

        /// <summary>
        /// Builds a business object bound to the test repository, with additional service
        /// overrides layered on top. Used by the audit tests to enable
        /// <c>AuditLogOptions</c> and capture what <c>IAuditLogWriter</c> receives.
        /// </summary>
        public FormBusinessObject CreateBoWithOverrides(params (Type ServiceType, object? Instance)[] overrides)
        {
            var all = new List<(Type, object?)>
            {
                (typeof(IRepositoryFactory), new StubFactory(_repository))
            };
            all.AddRange(overrides);
            return new FormBusinessObject(
                TestBeeContext.CreateWithOverrides(_fx, [.. all]), Guid.NewGuid(), ProgId);
        }

        private sealed class StubFactory : IRepositoryFactory
        {
            private readonly IDataFormRepository _repository;
            public StubFactory(IDataFormRepository repository) => _repository = repository;
            public T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository => (T)_repository;
            public T Create<T>(Guid accessToken = default) where T : class
                => throw new NotSupportedException();
        }
    }
}
