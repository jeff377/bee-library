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
    /// Dependencies are supplied via constructor injection by the host DI container. The injected
    /// <see cref="IServiceProvider"/> is the same provider that backs <see cref="IBeeContext.Services"/>
    /// — it is forwarded to every BO instance so the rare escape-hatch resolutions (login-only
    /// helpers etc.) reach the host's request scope.
    /// </remarks>
    public class BusinessObjectFactory : IBusinessObjectFactory
    {
        private readonly IServiceProvider _services;
        private readonly IDefineAccess _defineAccess;
        private readonly ISessionInfoService _sessionInfoService;
        private readonly IFormBoTypeResolver _resolver;

        /// <summary>
        /// Initializes a new <see cref="BusinessObjectFactory"/>.
        /// </summary>
        /// <param name="services">The host service provider used as the BO escape hatch.</param>
        /// <param name="defineAccess">The define access service.</param>
        /// <param name="sessionInfoService">The session info access service.</param>
        /// <param name="resolver">The progId → BO type resolver.</param>
        public BusinessObjectFactory(
            IServiceProvider services,
            IDefineAccess defineAccess,
            ISessionInfoService sessionInfoService,
            IFormBoTypeResolver resolver)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _sessionInfoService = sessionInfoService ?? throw new ArgumentNullException(nameof(sessionInfoService));
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

        private IBeeContext BuildContext() => new BeeContext
        {
            DefineAccess = _defineAccess,
            SessionInfoService = _sessionInfoService,
            BoFactory = this,
            Services = _services,
        };
    }
}
