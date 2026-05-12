using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Storage;

namespace Bee.Business
{
    /// <summary>
    /// Default implementation of <see cref="IBusinessObjectFactory"/>; creates business logic objects
    /// (<see cref="SystemBusinessObject"/> or <see cref="FormBusinessObject"/>) for incoming API calls.
    /// </summary>
    /// <remarks>
    /// Cross-cutting services for the per-call <see cref="IBeeContext"/> are installed via
    /// <see cref="Initialize(IDefineAccess, ISessionInfoService)"/>, called by
    /// <c>BackendInfo.Initialize</c> via reflection. The <c>Services</c> escape hatch
    /// is backed by <see cref="BackendInfoServiceProvider"/> until Phase 4 swaps for a real DI scope.
    /// </remarks>
    public class BusinessObjectFactory : IBusinessObjectFactory
    {
        private static IDefineAccess? _defineAccess;
        private static ISessionInfoService? _sessionInfoService;
        private static readonly IServiceProvider _services = new BackendInfoServiceProvider();

        private readonly IFormBoTypeResolver _resolver;

        /// <summary>
        /// Installs the cross-cutting services used to build per-call <see cref="IBeeContext"/>.
        /// Must be called once at host startup before any BO is created; typically invoked by
        /// <c>BackendInfo.Initialize</c>.
        /// </summary>
        /// <param name="defineAccess">The define access service.</param>
        /// <param name="sessionInfoService">The session-info access service.</param>
        public static void Initialize(IDefineAccess defineAccess, ISessionInfoService sessionInfoService)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
        }

        /// <summary>
        /// Initializes a new <see cref="BusinessObjectFactory"/> using the default
        /// <see cref="DefaultFormBoTypeResolver"/>.
        /// </summary>
        public BusinessObjectFactory() : this(new DefaultFormBoTypeResolver())
        { }

        /// <summary>
        /// Initializes a new <see cref="BusinessObjectFactory"/> with a custom resolver.
        /// </summary>
        /// <param name="resolver">The progId → BO type resolver.</param>
        public BusinessObjectFactory(IFormBoTypeResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Creates a system-level business logic object.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public object CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true)
        {
            var ctx = BuildContext();
            return new SystemBusinessObject(ctx, accessToken, isLocalCall);
        }

        /// <summary>
        /// Creates a form-level business logic object.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public object CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
        {
            var type = _resolver.Resolve(progId);
            var ctx = BuildContext();
            return Activator.CreateInstance(type, ctx, accessToken, progId, isLocalCall)!;
        }

        private BeeContext BuildContext()
        {
            if (_defineAccess == null || _sessionInfoService == null)
                throw new InvalidOperationException(
                    "BusinessObjectFactory has not been initialized. Call BusinessObjectFactory.Initialize(defineAccess, sessionInfoService) at startup (BackendInfo.Initialize handles this).");

            return new BeeContext
            {
                DefineAccess = _defineAccess,
                SessionInfoService = _sessionInfoService,
                BoFactory = this,
                Services = _services,
            };
        }
    }
}
