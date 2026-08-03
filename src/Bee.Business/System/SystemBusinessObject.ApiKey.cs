using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Base.Security;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Business.System
{
    /// <summary>
    /// API key issuing half of <see cref="SystemBusinessObject"/>. Split out for file size only.
    /// </summary>
    public partial class SystemBusinessObject
    {
        /// <summary>
        /// Issues a new API key and returns the complete plaintext key once.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// IMPORTANT: the returned key cannot be produced again. Only a salted hash of the secret is
        /// stored, so losing the value means issuing a replacement and retiring this one — which is
        /// the rotation procedure in any case.
        /// <para>
        /// An API key belongs to the installation rather than to any company, so a remote caller is
        /// gated on <see cref="IDeploymentAuthorizationService"/> rather than on company roles:
        /// being merely authenticated has never been enough to mint a credential, and a company
        /// administrator must not gain that ability either. Local calls pass without an
        /// administrator, which is what keeps the bootstrap path open — a deployment with no
        /// administrator yet has to be able to mint its first key on the host.
        /// </para>
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual CreateApiKeyResult CreateApiKey(CreateApiKeyArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            // Authorization first: nothing about the request is worth validating if the caller may
            // not mint keys at all.
            if (!IsLocalCall &&
                !Services.GetRequiredService<IDeploymentAuthorizationService>()
                         .Can(AccessToken, DeploymentAction.ManageApiKey))
            {
                throw new UnauthorizedAccessException("Not authorized to issue API keys.");
            }

            if (!ApiKeyFormat.IsValidSysId(args.SysId))
            {
                throw new UserMessageException(
                    $"Invalid API key id. Use {ApiKeyFormat.MinSysIdLength}-{ApiKeyFormat.MaxSysIdLength} " +
                    "characters of lowercase letters, digits and hyphens, not starting or ending with a hyphen.");
            }
            if (StringUtilities.IsEmpty(args.SysName))
            {
                throw new UserMessageException("An application name is required for an API key.");
            }
            if (args.ExpiredAt.HasValue && args.ExpiredAt.Value <= DateTime.UtcNow)
            {
                throw new UserMessageException("The expiry time must be in the future.");
            }

            var repository = Services.GetRequiredService<ISystemRepositoryFactory>().CreateApiKeyRepository();
            // Checked up front so a duplicate id reports something actionable instead of surfacing a
            // unique-index violation from the provider.
            if (repository.Exists(args.SysId))
            {
                throw new UserMessageException($"An API key with id '{args.SysId}' already exists.");
            }

            string secret = ApiKeyFormat.CreateSecret();
            repository.Insert(new ApiKeyInfo
            {
                SysId = args.SysId,
                SysName = args.SysName,
                HashedKey = ApiKeyHasher.HashSecret(secret),
                KeyType = args.KeyType,
                Contact = args.Contact ?? string.Empty,
                ExpiredAt = args.ExpiredAt,
            });

            return new CreateApiKeyResult
            {
                SysId = args.SysId,
                ApiKey = ApiKeyFormat.Compose(args.SysId, secret),
            };
        }
    }
}
