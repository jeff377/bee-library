using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Security;

namespace Bee.Business.Form
{
    /// <summary>
    /// Form-level business logic object.
    /// </summary>
    public partial class FormBusinessObject : BusinessObject, IFormBusinessObject
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FormBusinessObject"/> class.
        /// </summary>
        /// <param name="ctx">The per-call context aggregating cross-cutting services.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public FormBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
            : base(ctx, accessToken, progId, isLocalCall)
        { }

        #endregion

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFunc"/>.
        /// </summary>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, IsLocalCall, args, result);
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFuncAnonymous"/>.
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, IsLocalCall, args, result);
        }

        /// <summary>
        /// Builds the plugin runner for one <c>Save</c> or <c>Delete</c> call.
        /// </summary>
        /// <remarks>
        /// One runner per call, which is what makes the plugin instances per operation: every stage
        /// of this call goes through the same runner and therefore the same objects.
        /// </remarks>
        private FormPluginRunner CreatePluginRunner()
            => PluginResolver.Resolve(GetCurrentCustomizeId(), ProgId)
                .CreateRunner(Context, AccessToken, ProgId);

        /// <summary>
        /// Returns true when the schema declares any enabled <c>BeforeDelete</c> rule.
        /// </summary>
        private static bool HasBeforeDeleteRules(FormSchema schema)
            => schema.Rules != null &&
               schema.Rules.Any(r => r.Enabled && r.Trigger == FormRuleTrigger.BeforeDelete);

        /// <summary>
        /// Builds the rounding context used to round computed numeric fields, from the current
        /// session's company and the currency/unit settings.
        /// </summary>
        private RoundingContext BuildRoundingContext()
        {
            return new RoundingContext
            {
                Company = ResolveCompanyInfo(),
                CurrencySettings = DefineAccess.GetCurrencySettings(),
                UnitSettings = DefineAccess.GetUnitSettings(),
            };
        }

        /// <summary>
        /// Resolves the requesting user's IANA time zone id, or an empty string when the token has
        /// no session (meaning UTC).
        /// </summary>
        private string ResolveSessionTimeZone()
            => SessionInfoService.Get(AccessToken)?.TimeZone ?? string.Empty;

        /// <summary>
        /// Resolves the current session's <see cref="CompanyInfo"/>, or null when no company is bound.
        /// </summary>
        private CompanyInfo? ResolveCompanyInfo()
        {
            var session = SessionInfoService.Get(AccessToken);
            if (session == null || string.IsNullOrEmpty(session.CompanyId)) { return null; }
            return Services.GetService<ICompanyInfoService>()?.Get(session.CompanyId);
        }
    }
}
