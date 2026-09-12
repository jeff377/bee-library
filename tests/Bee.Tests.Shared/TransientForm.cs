using System.Data.Common;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Form;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// A form whose <see cref="FormSchema"/> the test builds and whose tables exist only while the test
    /// runs, on one provider's company database.
    /// </summary>
    /// <remarks>
    /// Business objects built from <see cref="CreateContext"/> resolve the test's schema and repository
    /// while every other service comes from the fixture. The tables are created through
    /// <see cref="TableSchemaBuilder"/>, so each provider gets the column types and defaults the framework
    /// itself would give them.
    /// </remarks>
    public sealed class TransientForm
    {
        /// <summary>
        /// The database category the tables are created in.
        /// </summary>
        public const string CategoryId = "company";

        private readonly SharedDbFixture _fx;

        /// <summary>
        /// Initializes a new instance of <see cref="TransientForm"/>.
        /// </summary>
        /// <param name="fx">The shared database fixture.</param>
        /// <param name="databaseType">The provider to create the tables on.</param>
        /// <param name="schema">The form schema; its table names must be unique to the test.</param>
        public TransientForm(SharedDbFixture fx, DatabaseType databaseType, FormSchema schema)
        {
            _fx = fx ?? throw new ArgumentNullException(nameof(fx));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            DatabaseType = databaseType;
            DatabaseId = TestDbConventions.GetDatabaseId(databaseType, CategoryId);
            DefineAccess = new FormSchemaOverlayDefineAccess(fx.GetRequiredService<IDefineAccess>(), schema);
            DbAccess = fx.NewDbAccess(DatabaseId);
            Repository = new DataFormRepository(
                TestRepositoryContext.Create(
                    fx.GetRequiredService<IDbConnectionManager>(),
                    defineAccess: DefineAccess,
                    dbAccessFactory: fx.GetRequiredService<IDbAccessFactory>()),
                schema.ProgId, schema, DatabaseId);
        }

        /// <summary>
        /// Gets the provider the tables live on.
        /// </summary>
        public DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the database id the tables live in.
        /// </summary>
        public string DatabaseId { get; }

        /// <summary>
        /// Gets the form schema.
        /// </summary>
        public FormSchema Schema { get; }

        /// <summary>
        /// Gets the define access that serves <see cref="Schema"/> on top of the fixture's definitions.
        /// </summary>
        public IDefineAccess DefineAccess { get; }

        /// <summary>
        /// Gets a database access for direct SQL against the tables.
        /// </summary>
        public DbAccess DbAccess { get; }

        /// <summary>
        /// Gets the form repository bound to <see cref="Schema"/> and <see cref="DatabaseId"/>.
        /// </summary>
        public IDataFormRepository Repository { get; }

        /// <summary>
        /// Returns a table name that no other test run uses, short enough for Oracle identifiers.
        /// </summary>
        /// <param name="prefix">A short prefix naming the test.</param>
        public static string NewTableName(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

        /// <summary>
        /// Quotes an identifier for <see cref="DatabaseType"/>.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        public string Quote(string identifier) => DatabaseType.QuoteIdentifier(identifier);

        /// <summary>
        /// Creates every table the schema declares.
        /// </summary>
        public void CreateTables()
        {
            var builder = new TableSchemaBuilder(DatabaseId, DefineAccess, _fx.GetRequiredService<IDbConnectionManager>());
            foreach (var table in Schema.Tables!)
            {
                builder.Execute(CategoryId, table.GenerateDbTable().TableName);
            }
        }

        /// <summary>
        /// Drops every table the schema declares, details before the master. A failure is logged, not thrown.
        /// </summary>
        public void DropTables()
        {
            var tables = Schema.Tables!.ToList();
            for (int i = tables.Count - 1; i >= 0; i--)
            {
                var name = tables[i].GenerateDbTable().TableName;
                try
                {
                    DbAccess.ExecuteNonQuery($"DROP TABLE {Quote(name)}");
                }
                catch (DbException ex)
                {
                    Console.WriteLine($"TransientForm cleanup of '{name}' failed — {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Builds a context whose define access and repository factory point at this form.
        /// </summary>
        /// <param name="overrides">Further service overrides layered on the fixture's provider.</param>
        public IBeeContext CreateContext(params (Type ServiceType, object? Instance)[] overrides)
        {
            var all = new List<(Type, object?)> { (typeof(IRepositoryFactory), new RepositoryFactoryStub(Repository)) };
            all.AddRange(overrides);
            return new BeeContext
            {
                DefineAccess = DefineAccess,
                SessionInfoService = _fx.GetRequiredService<ISessionInfoService>(),
                LanguageService = _fx.GetRequiredService<ILanguageService>(),
                BoFactory = _fx.GetRequiredService<IBusinessObjectFactory>(),
                Services = new TestOverrideServiceProvider(_fx.Provider, [.. all]),
            };
        }

        private sealed class RepositoryFactoryStub : IRepositoryFactory
        {
            private readonly IDataFormRepository _repository;

            public RepositoryFactoryStub(IDataFormRepository repository) => _repository = repository;

            public T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository
                => (T)_repository;

            public T Create<T>(Guid accessToken = default) where T : class => throw new NotSupportedException();
        }
    }
}
