using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Base.Security;
using Bee.Business.AuditLog;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Security;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;

namespace Bee.Business.System
{
    /// <summary>
    /// API key issuing half of <see cref="SystemBusinessObject"/>. Split out for file size only.
    /// </summary>
    public partial class SystemBusinessObject
    {
        /// <summary>The common-database table holding issued API keys.</summary>
        private const string ApiKeyTableName = "st_api_key";

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
            RequireApiKeyManagement("issue API keys");

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

            var repository = Services.GetRequiredService<IRepositoryFactory>().Create<IApiKeyRepository>();
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

            if (DeploymentAuditEnabled())
            {
                // WARNING: the secret and its hash are absent from this list and must stay absent.
                // The log database is a separate store with its own (usually wider) readership, and
                // an audit row that carried the hash would put an offline-crackable credential
                // somewhere the credential itself never goes.
                string changes = AuditDiffGram.ForInsert(ApiKeyTableName,
                [
                    (SysFields.Id, args.SysId),
                    (SysFields.Name, args.SysName),
                    ("key_type", args.KeyType),
                    ("contact", args.Contact ?? string.Empty),
                    ("expired_at", args.ExpiredAt),
                ]);
                // The key's sys_id stands in for a row id: it is this row's identity, it is not a
                // secret, and the repository surfaces no row id to record instead.
                WriteDeploymentAudit(ApiKeyTableName, args.SysId, ChangeKind.Insert, changes,
                    SystemActions.CreateApiKey);
            }

            return new CreateApiKeyResult
            {
                SysId = args.SysId,
                ApiKey = ApiKeyFormat.Compose(args.SysId, secret),
            };
        }

        /// <summary>
        /// Lists the issued API keys, enabled and disabled alike.
        /// </summary>
        /// <param name="args">The input arguments; carries no criteria.</param>
        /// <remarks>
        /// IMPORTANT: returns <see cref="ApiKeySummary"/>, which has no credential material. The
        /// stored hash stays on the server — listing keys is an operator's view of what exists, not
        /// a way to read what was issued.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual ListApiKeysResult ListApiKeys(ListApiKeysArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            RequireApiKeyManagement("list API keys");

            var repository = Services.GetRequiredService<IRepositoryFactory>().Create<IApiKeyRepository>();
            return new ListApiKeysResult { ApiKeys = [.. repository.GetList()] };
        }

        /// <summary>
        /// Enables or disables an issued API key.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// IMPORTANT: disabling is the revocation path, and it takes effect at once — the repository
        /// announces the change in the same transaction as the write, so other processes drop their
        /// cached copy rather than honouring the key until it lapses.
        /// <para>
        /// Disabling the last enabled key takes the key gate out of force, after which any non-empty
        /// <c>X-Api-Key</c> is accepted again. That is the documented pre-gate behaviour rather than
        /// a failure, but it makes "disable the old key" the wrong last step of a rotation on a
        /// deployment that holds only one.
        /// </para>
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual SetApiKeyEnabledResult SetApiKeyEnabled(SetApiKeyEnabledArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            RequireApiKeyManagement("manage API keys");

            var repository = Services.GetRequiredService<IRepositoryFactory>().Create<IApiKeyRepository>();
            bool auditing = DeploymentAuditEnabled();
            // Read before writing so the audit records the direction; enable and disable are both an
            // Update and are otherwise indistinguishable.
            bool before = auditing && IsEnabled(repository, args.SysId);

            if (!repository.SetEnabled(args.SysId, args.Enabled))
            {
                throw new UserMessageException($"No API key with id '{args.SysId}' exists.");
            }

            if (auditing)
            {
                WriteDeploymentAudit(ApiKeyTableName, args.SysId, ChangeKind.Update,
                    AuditDiffGram.ForFieldUpdate(ApiKeyTableName, args.SysId, "enabled", before, args.Enabled,
                        [(SysFields.Id, args.SysId)]),
                    SystemActions.SetApiKeyEnabled);
            }

            return new SetApiKeyEnabledResult { SysId = args.SysId, Enabled = args.Enabled };
        }

        /// <summary>
        /// Sets or clears an issued API key's expiry.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// A past expiry is accepted here, unlike on <see cref="CreateApiKey"/>: issuing a key that
        /// is already dead is a mistake, whereas expiring a live one as of a moment that has passed
        /// is a legitimate way to retire it.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Encrypted, ApiAccessRequirement.Authenticated)]
        public virtual SetApiKeyExpiryResult SetApiKeyExpiry(SetApiKeyExpiryArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            RequireApiKeyManagement("manage API keys");

            var repository = Services.GetRequiredService<IRepositoryFactory>().Create<IApiKeyRepository>();
            bool auditing = DeploymentAuditEnabled();
            DateTime? before = auditing ? FindExpiry(repository, args.SysId) : null;

            if (!repository.SetExpiry(args.SysId, args.ExpiredAt))
            {
                throw new UserMessageException($"No API key with id '{args.SysId}' exists.");
            }

            if (auditing)
            {
                WriteDeploymentAudit(ApiKeyTableName, args.SysId, ChangeKind.Update,
                    AuditDiffGram.ForFieldUpdate(ApiKeyTableName, args.SysId, "expired_at", before, args.ExpiredAt,
                        [(SysFields.Id, args.SysId)]),
                    SystemActions.SetApiKeyExpiry);
            }

            return new SetApiKeyExpiryResult { SysId = args.SysId, ExpiredAt = args.ExpiredAt };
        }

        /// <summary>
        /// Rejects a caller who may not manage API keys.
        /// </summary>
        /// <param name="what">The attempted operation, as it reads in the rejection message.</param>
        /// <exception cref="UnauthorizedAccessException">The caller is not authorized.</exception>
        /// <remarks>
        /// WARNING: a local call passes without an administrator, and that is deliberate — it is the
        /// bootstrap path a deployment with no administrator yet depends on. Removing the
        /// <see cref="BusinessObject.IsLocalCall"/> branch as a "hardening" would lock such a
        /// deployment out of its own key management.
        /// </remarks>
        private void RequireApiKeyManagement(string what)
        {
            if (IsLocalCall) { return; }

            if (!Services.GetRequiredService<IDeploymentAuthorizationService>()
                         .Can(AccessToken, DeploymentAction.ManageApiKey))
            {
                throw new UnauthorizedAccessException($"Not authorized to {what}.");
            }
        }

        /// <summary>
        /// Reads a key's current enabled state for the audit before-image.
        /// </summary>
        private static bool IsEnabled(Repository.Abstractions.System.IApiKeyRepository repository, string sysId)
            => Find(repository, sysId)?.Enabled ?? false;

        /// <summary>
        /// Reads a key's current expiry for the audit before-image.
        /// </summary>
        private static DateTime? FindExpiry(Repository.Abstractions.System.IApiKeyRepository repository, string sysId)
            => Find(repository, sysId)?.ExpiredAt;

        /// <summary>
        /// Finds a key's summary by identifier, including disabled rows.
        /// </summary>
        /// <remarks>
        /// NOTE: <c>GetEnabledById</c> cannot serve here — it filters disabled rows out, so
        /// re-enabling a key would read its before-image as "not found" and the audit would claim
        /// the key was created rather than changed.
        /// </remarks>
        private static ApiKeySummary? Find(Repository.Abstractions.System.IApiKeyRepository repository, string sysId)
            => repository.GetList().FirstOrDefault(k => string.Equals(k.SysId, sysId, StringComparison.Ordinal));
    }
}
