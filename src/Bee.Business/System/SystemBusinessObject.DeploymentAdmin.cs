using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Business.AuditLog;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Logging;
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
        /// <summary>The common-database table holding the administrator flag.</summary>
        private const string UserTableName = "st_user";

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

            // Read before writing, and only when the value will actually be recorded: the audit row
            // has to say which direction this went — grant and revoke are both an Update, so without
            // the before value the trail cannot tell them apart.
            bool auditing = DeploymentAuditEnabled();
            bool before = auditing && repository.IsDeploymentAdmin(args.UserId);

            if (!repository.SetDeploymentAdmin(args.UserId, args.IsDeploymentAdmin))
            {
                throw new UserMessageException($"No user with id '{args.UserId}' exists.");
            }

            if (auditing)
            {
                string rowKey = repository.GetRowIdBySysId(args.UserId).ToString();
                // The affected user is the row key; the business id rides along as context so the
                // entry names someone rather than only a row identifier.
                string changes = AuditDiffGram.ForFieldUpdate(UserTableName, rowKey,
                    ProtectedFields.DeploymentAdmin, before, args.IsDeploymentAdmin,
                    [(SysFields.Id, args.UserId)]);
                WriteDeploymentAudit(UserTableName, rowKey, ChangeKind.Update, changes,
                    SystemActions.SetDeploymentAdmin);
            }

            return new SetDeploymentAdminResult
            {
                UserId = args.UserId,
                IsDeploymentAdmin = args.IsDeploymentAdmin,
            };
        }
    }
}
