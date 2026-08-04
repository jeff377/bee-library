using Bee.Base;
using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Abstractions.System;
using Bee.Repository.AuditLog;
using Bee.Repository.Form;
using Bee.Repository.System;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default <see cref="IRepositoryFactory"/>: builds every repository, on both axes, from one
    /// shared <see cref="IRepositoryContext"/>.
    /// </summary>
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly IRepositoryContext _ctx;
        private readonly IServiceProvider _services;

        /// <summary>
        /// The framework axis, as data. These repositories have fixed types and no progId, so the
        /// mapping is a table rather than a method apiece — which is what stops this class growing
        /// one member per system table the way its predecessor did.
        /// </summary>
        private static readonly Dictionary<Type, Type> s_frameworkTypes = new()
        {
            [typeof(ISessionRepository)] = typeof(SessionRepository),
            [typeof(ICompanyRepository)] = typeof(CompanyRepository),
            [typeof(IUserCompanyRepository)] = typeof(UserCompanyRepository),
            [typeof(IUserRepository)] = typeof(UserRepository),
            [typeof(IApiKeyRepository)] = typeof(ApiKeyRepository),
            [typeof(IDatabaseRepository)] = typeof(DatabaseRepository),
            [typeof(IRolePermissionRepository)] = typeof(RolePermissionRepository),
            [typeof(IDepartmentRepository)] = typeof(DepartmentRepository),
            [typeof(IEmployeeRepository)] = typeof(EmployeeRepository),
            [typeof(IAuditLogRepository)] = typeof(AuditLogRepository),
            [typeof(IAuditLogWriteRepository)] = typeof(AuditLogWriteRepository),
            [typeof(IReportFormRepository)] = typeof(ReportFormRepository),
        };

        /// <summary>
        /// Initializes a new <see cref="RepositoryFactory"/>.
        /// </summary>
        /// <param name="services">The host service provider, used for the escape hatch and for injecting a custom repository's own dependencies.</param>
        /// <param name="defineAccess">The define access service.</param>
        /// <param name="dbAccessFactory">The database access factory.</param>
        /// <param name="connectionManager">The connection manager.</param>
        /// <param name="router">Resolves a logical scope to a physical database id.</param>
        /// <param name="cacheNotify">Cross-process cache invalidation channel; <c>null</c> when the host does not poll it.</param>
        public RepositoryFactory(
            IServiceProvider services,
            IDefineAccess defineAccess,
            IDbAccessFactory dbAccessFactory,
            IDbConnectionManager connectionManager,
            IRepositoryDatabaseRouter router,
            ICacheNotifyService? cacheNotify = null)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ctx = new RepositoryContext
            {
                DefineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess)),
                DbAccessFactory = dbAccessFactory ?? throw new ArgumentNullException(nameof(dbAccessFactory)),
                ConnectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager)),
                Router = router ?? throw new ArgumentNullException(nameof(router)),
                CacheNotify = cacheNotify,
                Services = services,
            };
        }

        /// <summary>Gets the context handed to every repository this factory builds.</summary>
        protected IRepositoryContext Context => _ctx;

        /// <inheritdoc/>
        public T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);

            var repository = CreateFormRepositoryCore(accessToken, progId);
            return repository as T
                ?? throw new InvalidOperationException(
                    $"The repository for progId '{progId}' is '{repository.GetType().FullName}', " +
                    $"which does not implement {typeof(T).FullName}.");
        }

        /// <summary>
        /// Builds the repository bound to a progId. Overridable so a host can take over the whole
        /// resolution without reimplementing the framework axis alongside it.
        /// </summary>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">The program identifier.</param>
        protected virtual IDataFormRepository CreateFormRepositoryCore(Guid accessToken, string progId)
            => new DataFormRepository(_ctx, accessToken, progId);

        /// <inheritdoc/>
        public T Create<T>(Guid accessToken = default) where T : class
        {
            if (!s_frameworkTypes.TryGetValue(typeof(T), out var implementation))
            {
                throw new NotSupportedException(
                    $"No framework repository is registered for {typeof(T).FullName}. " +
                    "Repositories bound to a progId are created through CreateFormRepository instead.");
            }

            return (T)ActivatorUtilities.CreateInstance(_services, implementation, _ctx, accessToken, string.Empty);
        }

        /// <summary>
        /// Maps a form schema's category to the logical database scope it means at runtime.
        /// </summary>
        /// <param name="categoryId">The <c>FormSchema.CategoryId</c> value.</param>
        /// <exception cref="InvalidOperationException">Thrown when the category is not one the framework recognises.</exception>
        internal static DbScope ParseCategoryId(string categoryId)
            => categoryId switch
            {
                DbCategoryIds.Common => DbScope.Common,
                DbCategoryIds.Company => DbScope.Company,
                DbCategoryIds.Log => DbScope.Log,
                _ => throw new InvalidOperationException(
                    $"Unknown schema.CategoryId '{categoryId}'.")
            };

        /// <summary>
        /// Reads and validates the form schema a repository will be built against.
        /// </summary>
        /// <param name="defineAccess">The define access service.</param>
        /// <param name="progId">The program identifier.</param>
        internal static Definition.Forms.FormSchema LoadSchema(IDefineAccess defineAccess, string progId)
        {
            var schema = defineAccess.GetFormSchema(progId);
            if (StringUtilities.IsEmpty(schema.CategoryId))
            {
                throw new InvalidOperationException(
                    $"FormSchema '{progId}' does not specify a CategoryId; cannot resolve target database.");
            }
            return schema;
        }
    }
}
