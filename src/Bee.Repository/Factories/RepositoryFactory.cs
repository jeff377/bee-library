using Bee.Base;
using Bee.Db;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Customization;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
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
        private readonly ICustomizeDefineReader? _customizeReader;
        private readonly ISessionInfoService? _sessionInfoService;

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
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the overlay, so every progId resolves against the base registry.</param>
        /// <param name="sessionInfoService">Reads the session's customization code; <c>null</c> has the same effect as a host with no sessions — the base registry applies.</param>
        public RepositoryFactory(
            IServiceProvider services,
            IDefineAccess defineAccess,
            IDbAccessFactory dbAccessFactory,
            IDbConnectionManager connectionManager,
            IRepositoryDatabaseRouter router,
            ICacheNotifyService? cacheNotify = null,
            ICustomizeDefineReader? customizeReader = null,
            ISessionInfoService? sessionInfoService = null)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _customizeReader = customizeReader;
            _sessionInfoService = sessionInfoService;
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
        {
            var type = ResolveFormRepositoryType(accessToken, progId);
            return type == typeof(DataFormRepository)
                ? new DataFormRepository(_ctx, accessToken, progId)
                : (IDataFormRepository)ActivatorUtilities.CreateInstance(_services, type, _ctx, accessToken, progId);
        }

        /// <summary>
        /// Resolves the repository type registered for a progId in <c>ProgramSettings</c>, applying
        /// the tenant customization overlay.
        /// </summary>
        /// <param name="accessToken">The current request's access token, used only to read the session's customization code.</param>
        /// <param name="progId">The program identifier.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the registry names a type that will not load, or one that does not derive
        /// from <see cref="DataFormRepository"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// <b>Every failure throws</b> — deliberately the opposite of the business-object axis,
        /// where an unloadable name degrades to the generic CRUD object. There the cost of a typo
        /// in <c>Order</c> is a program that behaves generically; here it would be reads and writes
        /// running through the framework's own SQL after an author replaced that logic on purpose.
        /// A fallback would not avert the failure, only postpone it to a point where the data is
        /// already wrong.
        /// </para>
        /// <para>
        /// Unlike the business-object resolver this holds no type cache, so a definition reload
        /// takes effect on the next call with no invalidation machinery. What that costs is one
        /// <see cref="AssemblyLoader.GetType(string)"/> per creation, and its expensive half — the
        /// assembly load — is already cached inside <see cref="AssemblyLoader"/>.
        /// </para>
        /// </remarks>
        protected Type ResolveFormRepositoryType(Guid accessToken, string progId)
        {
            var item = FindProgramItem(accessToken, progId);
            if (item == null || StringUtilities.IsEmpty(item.Repository))
                return typeof(DataFormRepository);

            Type? type;
            try
            {
                type = AssemblyLoader.GetType(item.Repository);
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                throw UnloadableRepository(progId, item.Repository, ex);
            }

            if (type == null)
                throw UnloadableRepository(progId, item.Repository, inner: null);

            if (!typeof(DataFormRepository).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    $"ProgramSettings binds progId '{progId}' to repository '{item.Repository}', " +
                    $"which does not derive from {typeof(DataFormRepository).FullName}. " +
                    "A form repository must extend the framework's own so the CRUD surface stays intact.");
            }

            return type;
        }

        /// <summary>
        /// Reads the registry entry for a progId, letting a tenant customization entry replace the
        /// base one outright — the same per-progId granularity the business-object axis uses, and
        /// the same <see cref="CustomizeOverlay"/> a client runs, so both ends agree.
        /// </summary>
        /// <param name="accessToken">The current request's access token.</param>
        /// <param name="progId">The program identifier.</param>
        private ProgramItem? FindProgramItem(Guid accessToken, string progId)
        {
            ProgramSettings? baseSettings;
            try
            {
                baseSettings = _ctx.DefineAccess.GetProgramSettings();
            }
            catch (FileNotFoundException)
            {
                // No ProgramSettings.xml means no progId is bound to anything; a customization entry
                // may still apply below. Same tolerance as the business-object resolver, so a host
                // can adopt the binding feature without shipping a base registry first.
                baseSettings = null;
            }

            string customizeId = GetCustomizeId(accessToken);
            ProgramSettings? custSettings = null;
            if (StringUtilities.IsNotEmpty(customizeId) && _customizeReader is not null)
                custSettings = _customizeReader.GetCustomizeProgramSettings(customizeId);

            return CustomizeOverlay.FindProgramItem(custSettings, baseSettings, progId);
        }

        /// <summary>
        /// Reads the session's tenant customization code. The session is the only accepted source:
        /// the code selects which tenant's definition files are read, so honouring a caller-supplied
        /// value would be a cross-tenant read.
        /// </summary>
        /// <param name="accessToken">The access token identifying the session.</param>
        private string GetCustomizeId(Guid accessToken)
        {
            if (accessToken == Guid.Empty || _sessionInfoService == null)
                return string.Empty;
            return _sessionInfoService.Get(accessToken)?.CustomizeId ?? string.Empty;
        }

        private static InvalidOperationException UnloadableRepository(string progId, string typeName, Exception? inner)
            => new(
                $"ProgramSettings binds progId '{progId}' to repository '{typeName}', which cannot be loaded. " +
                "Fix the assembly-qualified type name, or clear the attribute to use the framework default.",
                inner);

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
