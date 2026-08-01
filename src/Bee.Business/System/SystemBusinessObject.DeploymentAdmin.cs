using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Security;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Business.System
{
    /// <summary>
    /// Deployment administrator half of <see cref="SystemBusinessObject"/>. Split out for file size
    /// only.
    /// </summary>
    public partial class SystemBusinessObject
    {
        /// <summary>
        /// Grants or revokes a user's deployment administrator flag.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// WARNING: this is the only write path to <c>st_user.deployment_admin</c>, and a deployment
        /// administrator may act on installation-wide assets such as API keys. It is therefore
        /// <see cref="ApiProtectionLevel.LocalOnly"/>, on the same reasoning as <c>SaveDefine</c>
        /// and <c>CreateApiKey</c>: appointing an administrator is a deployment-time operation, and
        /// a merely authenticated account must not be able to appoint itself.
        /// <para>
        /// A new deployment gets its first administrator from the seed data instead; this method is
        /// how an existing deployment appoints one, on the host, after which appointments can be
        /// made through whatever administration surface the deployment builds on top of it.
        /// </para>
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Authenticated)]
        public virtual SetDeploymentAdminResult SetDeploymentAdmin(SetDeploymentAdminArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            // Defence in depth, as in SaveDefine: ApiAccessValidator only runs on the JSON-RPC
            // dispatch path, so a caller constructing the BO directly never passes through it.
            if (!IsLocalCall)
                throw new NotSupportedException("SetDeploymentAdmin is restricted to local calls.");

            if (StringUtilities.IsEmpty(args.UserId))
            {
                throw new UserMessageException("A user id is required.");
            }

            var repository = Services.GetRequiredService<ISystemRepositoryFactory>().CreateUserRepository();
            if (!repository.SetDeploymentAdmin(args.UserId, args.IsDeploymentAdmin))
            {
                throw new UserMessageException($"No user with id '{args.UserId}' exists.");
            }

            return new SetDeploymentAdminResult
            {
                UserId = args.UserId,
                IsDeploymentAdmin = args.IsDeploymentAdmin,
            };
        }
    }
}
