using Bee.Base;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Repository.Abstractions.Factories;
using Bee.Definition.Security;

namespace Bee.Business.System
{
    /// <summary>
    /// System-level business logic object.
    /// </summary>
    public partial class SystemBusinessObject : BusinessObject, ISystemBusinessObject
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemBusinessObject"/> class.
        /// </summary>
        /// <param name="ctx">The per-call context aggregating cross-cutting services.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier; always <see cref="SysProgIds.System"/>, accepted for signature uniformity and not read.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public SystemBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = false)
            : base(ctx, accessToken, progId, isLocalCall)
        { }

        #endregion

        /// <summary>
        /// Ping method for testing whether the API service is available.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual PingResult Ping(PingArgs args)
        {
            var apiKey = ApiKeyValidation;
            return new PingResult()
            {
                Status = "ok",
                ServerTime = DateTime.UtcNow,
                ApiKeyStatus = apiKey.Status,
                // Withheld unless the caller got past the key gate, so an unauthenticated probe
                // cannot read the framework version off a method that requires no key. `IsAccepted`
                // also covers the two states where the gate is not in force (no key issued yet, or an
                // in-process call), which keeps existing monitors working.
                Version = apiKey.IsAccepted ? SysInfo.Version : null,
                TraceId = args.TraceId // echo back the trace ID
            };
        }

        /// <summary>
        /// Gets common parameters and environment configuration.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual GetCommonConfigurationResult GetCommonConfiguration(GetCommonConfigurationArgs args)
        {
            var settings = DefineAccess.GetSystemSettings();
            var commonConfiguration = settings.CommonConfiguration;
            return new GetCommonConfigurationResult()
            {
                CommonConfiguration = commonConfiguration.ToXml()
            };
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFunc"/>.
        /// </summary>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new SystemExecFuncHandler(AccessToken, Services.GetRequiredService<IRepositoryFactory>());
            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, IsLocalCall, args, result);
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFuncAnonymous"/>.
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new SystemExecFuncHandler(AccessToken, Services.GetRequiredService<IRepositoryFactory>());
            handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, IsLocalCall, args, result);
        }
    }
}
