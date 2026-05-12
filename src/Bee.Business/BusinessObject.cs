using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Storage;

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

        /// <summary>Gets the business-object factory for BO-to-BO calls.</summary>
        protected IBusinessObjectFactory BoFactory => _ctx.BoFactory;

        /// <summary>
        /// Escape hatch for resolving services not in the typed core members
        /// (e.g. login-only helpers). Use sparingly; greppable for audit.
        /// </summary>
        protected IServiceProvider Services => _ctx.Services;

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
