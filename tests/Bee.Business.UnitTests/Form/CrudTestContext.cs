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

            _repository = new DataFormRepository(
                ProgId,
                EmployeeSchema,
                defineAccess,
                fx.GetRequiredService<IDbAccessFactory>(),
                fx.GetRequiredService<IDbConnectionManager>(),
                _databaseId);
        }

        public DatabaseType DbType { get; }
        public DbAccess DbAccess { get; }
        public FormSchema EmployeeSchema { get; }
        public IDataFormRepository Repository => _repository;

        public FormBusinessObject CreateBo()
        {
            var factory = new StubFactory(_repository);
            var ctx = TestBeeContext.CreateWithOverrides(_fx, (typeof(IFormRepositoryFactory), factory));
            return new FormBusinessObject(ctx, Guid.NewGuid(), ProgId);
        }

        private sealed class StubFactory : IFormRepositoryFactory
        {
            private readonly IDataFormRepository _repository;
            public StubFactory(IDataFormRepository repository) => _repository = repository;
            public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken) => _repository;
            public IReportFormRepository CreateReportFormRepository(string progId)
                => throw new NotSupportedException();
        }
    }
}
