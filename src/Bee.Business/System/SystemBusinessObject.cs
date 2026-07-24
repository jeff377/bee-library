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
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemBusinessObject"/> class.
        /// </summary>
        /// <param name="ctx">The per-call context aggregating cross-cutting services.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public SystemBusinessObject(IBeeContext ctx, Guid accessToken, bool isLocalCall = true)
            : base(ctx, accessToken, isLocalCall)
        { }

        #endregion

        /// <summary>
        /// Ping method for testing whether the API service is available.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual PingResult Ping(PingArgs args)
        {
            return new PingResult()
            {
                Status = "ok",
                ServerTime = DateTime.UtcNow,
                Version = SysInfo.Version, // system version
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
        /// Checks whether a newer version of the package is available.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Encoded, ApiAccessRequirement.Anonymous)]
        public virtual CheckPackageUpdateResult CheckPackageUpdate(CheckPackageUpdateArgs args)
        {
            // Implemented in derived classes.
            throw new NotSupportedException("CheckPackageUpdate is not implemented in the base class.");
        }

        /// <summary>
        /// Gets package information.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Encoded, ApiAccessRequirement.Anonymous)]
        public virtual GetPackageResult GetPackage(GetPackageArgs args)
        {
            // Implemented in derived classes.
            throw new NotSupportedException("GetPackage is not implemented in the base class.");
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFunc"/>.
        /// </summary>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new SystemExecFuncHandler(AccessToken, Services.GetRequiredService<ISystemRepositoryFactory>());
            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result);
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFuncAnonymous"/>.
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new SystemExecFuncHandler(AccessToken, Services.GetRequiredService<ISystemRepositoryFactory>());
            handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, args, result);
        }
    }
}
