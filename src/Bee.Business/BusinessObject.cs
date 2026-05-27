using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Identity;
using Bee.Definition.Language;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;

namespace Bee.Business
{
    /// <summary>
    /// Base class for business logic objects.
    /// </summary>
    public abstract class BusinessObject : IBusinessObject
    {
        private readonly IBeeContext _ctx;

        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessObject"/> class.
        /// </summary>
        /// <param name="ctx">The per-call context aggregating cross-cutting services.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        protected BusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall = true)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            AccessToken = accessToken;
            IsLocalCall = isLocalCall;
        }

        #endregion

        /// <summary>
        /// Gets the access token.
        /// </summary>
        public Guid AccessToken { get; }

        /// <summary>
        /// Gets the session information.
        /// </summary>
        public SessionInfo? SessionInfo { get; }

        /// <summary>
        /// Gets a value indicating whether the call originates from a local source (e.g., same process or host as the server).
        /// </summary>
        public bool IsLocalCall { get; } = false;

        /// <summary>Gets the definition data access service from the per-call context.</summary>
        protected IDefineAccess DefineAccess => _ctx.DefineAccess;

        /// <summary>Gets the session-info access service from the per-call context.</summary>
        protected ISessionInfoService SessionInfoService => _ctx.SessionInfoService;

        /// <summary>Gets the language resource service from the per-call context.</summary>
        protected ILanguageService LanguageService => _ctx.LanguageService;

        /// <summary>Gets the business-object factory for BO-to-BO calls.</summary>
        protected IBusinessObjectFactory BoFactory => _ctx.BoFactory;

        /// <summary>
        /// Escape hatch for resolving services not in the typed core members
        /// (e.g. login-only helpers). Use sparingly; greppable for audit.
        /// </summary>
        protected IServiceProvider Services => _ctx.Services;

        /// <summary>
        /// Resolves the physical databaseId for the supplied <see cref="DbScope"/>,
        /// using the current <see cref="AccessToken"/> for the per-session lookup
        /// path (<see cref="DbScope.Company"/>).
        /// </summary>
        /// <param name="scope">The bo repo's access intent.</param>
        protected string ResolveDatabaseId(DbScope scope)
            => Services.GetRequiredService<IRepositoryDatabaseRouter>()
                       .Resolve(scope, AccessToken);

        /// <summary>
        /// Convenience wrapper around <see cref="IFormRepositoryFactory.CreateDataFormRepository"/>
        /// that auto-passes the current <see cref="AccessToken"/>.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        protected IDataFormRepository CreateDataFormRepository(string progId)
            => Services.GetRequiredService<IFormRepositoryFactory>()
                       .CreateDataFormRepository(progId, AccessToken);

        /// <summary>
        /// Resolves localized text for the given full key using the current session's
        /// language (<c>SessionInfo.Culture</c>), falling back to the system
        /// default language and then to the key itself if both miss.
        /// </summary>
        /// <param name="fullKey">The full key (<c>"{namespace}.{subKey}"</c>); split on the first <c>.</c>.</param>
        protected string GetLangText(string fullKey)
            => LanguageService.GetLangText(GetCurrentLang(), fullKey);

        /// <summary>
        /// Resolves localized text using an explicit namespace and sub-key, applying the
        /// same fall-back chain as <see cref="GetLangText(string)"/>.
        /// </summary>
        /// <param name="namespace">The resource namespace.</param>
        /// <param name="subKey">The sub-key within that namespace.</param>
        protected string GetLangText(string @namespace, string subKey)
            => LanguageService.GetLangText(GetCurrentLang(), @namespace, subKey);

        /// <summary>
        /// Reads the current session's BCP-47 language code from
        /// <c>SessionInfo.Culture</c>. Returns an empty string when no
        /// session is established yet (anonymous calls); <see cref="ILanguageService"/>
        /// then falls back through to the system default language.
        /// </summary>
        private string GetCurrentLang()
        {
            if (AccessToken == Guid.Empty)
                return string.Empty;
            return SessionInfoService.Get(AccessToken)?.Culture ?? string.Empty;
        }

        /// <summary>
        /// Executes a custom method; requires authentication.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public ExecFuncResult ExecFunc(ExecFuncArgs args)
        {
            var result = new ExecFuncResult();
            DoExecFunc(args, result);
            return result;
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="ExecFunc"/>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        protected virtual void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        { }

        /// <summary>
        /// Executes a custom method; allows anonymous access.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public ExecFuncResult ExecFuncAnonymous(ExecFuncArgs args)
        {
            var result = new ExecFuncResult();
            DoExecFuncAnonymous(args, result);
            return result;
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="ExecFuncAnonymous"/>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <param name="result">The output result.</param>
        protected virtual void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        { }
    }
}
