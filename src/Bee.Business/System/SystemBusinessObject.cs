using Bee.Base;
using Bee.Base.Security;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Bee.Definition.Identity;
using Bee.Definition.Security;

namespace Bee.Business.System
{
    /// <summary>
    /// System-level business logic object.
    /// </summary>
    public class SystemBusinessObject : BusinessObject, ISystemBusinessObject
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
        /// Performs the login operation.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual LoginResult Login(LoginArgs args)
        {
            // Rare per-method needs (ILoginAttemptTracker, IApiEncryptionKeyProvider) resolved
            // via IBeeContext.Services escape hatch — ILoginAttemptTracker is an optional service
            // apps register if they need brute-force protection.
            var tracker = Services.GetService<ILoginAttemptTracker>();

            // 0. Check if the account is locked out due to excessive failed attempts
            if (tracker != null && tracker.IsLockedOut(args.UserId))
                throw new UnauthorizedAccessException("Account is temporarily locked due to too many failed login attempts. Please try again later.");

            // 1. Authenticate credentials and retrieve the user name
            if (!AuthenticateUser(args, out var userName))
            {
                tracker?.RecordFailure(args.UserId);
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            // Clear failed attempt history on successful login
            tracker?.Reset(args.UserId);

            // 2. Generate an encryption key on login (may be shared or random)
            byte[] encryptionKey = Services.GetRequiredService<IApiEncryptionKeyProvider>()
                .GenerateKeyForLogin();

            // 3. Create SessionInfo and store it in the cache
            var sessionInfo = new SessionInfo
            {
                AccessToken = Guid.NewGuid(),
                UserId = args.UserId,
                UserName = userName,
                ExpiredAt = DateTime.UtcNow.AddHours(1),
                ApiEncryptionKey = encryptionKey
            };
            SessionInfoService.Set(sessionInfo);

            // 4. Return the encrypted key and access token
            string encryptedKey = string.Empty;
            if (StringUtilities.IsNotEmpty(args.ClientPublicKey))
            {
                encryptedKey = RsaCryptor.EncryptWithPublicKey(
                    Convert.ToBase64String(encryptionKey),
                    args.ClientPublicKey
                );
            }

            return new LoginResult
            {
                AccessToken = sessionInfo.AccessToken,
                ExpiredAt = sessionInfo.ExpiredAt,
                ApiEncryptionKey = encryptedKey,
                UserId = sessionInfo.UserId,
                UserName = sessionInfo.UserName,
            };
        }

        /// <summary>
        /// Enters the specified company for the current session. Also used to switch
        /// between companies — the previous <c>CompanyId</c> is overwritten.
        /// </summary>
        /// <param name="args">The input arguments carrying the target company id.</param>
        /// <remarks>
        /// Permission validation enforces three rules: (1) the target company exists,
        /// (2) it is enabled, (3) the current user is granted access via the
        /// <c>st_user_company</c> table. All three failure modes surface as the same
        /// <see cref="InvalidOperationException"/> with the message
        /// <c>"Company access denied."</c> so callers cannot enumerate companies by
        /// probing the error text.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual EnterCompanyResult EnterCompany(EnterCompanyArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.CompanyId))
                throw new ArgumentException("CompanyId is required.", nameof(args));

            var sessionInfo = SessionInfoService.Get(AccessToken)
                ?? throw new UnauthorizedAccessException("Session not found or has expired.");

            var companyInfoService = Services.GetRequiredService<ICompanyInfoService>();
            var companyInfo = companyInfoService.Get(args.CompanyId)
                ?? throw new InvalidOperationException("Company access denied.");

            var userCompanyRepository = Services.GetRequiredService<IUserCompanyRepository>();
            if (!userCompanyRepository.HasAccess(sessionInfo.UserId, args.CompanyId))
                throw new InvalidOperationException("Company access denied.");

            sessionInfo.CompanyId = args.CompanyId;
            SessionInfoService.Set(sessionInfo);

            return new EnterCompanyResult { Company = companyInfo };
        }

        /// <summary>
        /// Clears the company context from the current session while keeping the session alive.
        /// </summary>
        /// <param name="args">The input arguments (currently carries no fields).</param>
        /// <remarks>
        /// Idempotent — calling on a session that has never entered a company succeeds
        /// without error. To completely sign out, use <c>Logout</c> instead, which
        /// performs the same clear-up internally before destroying the session.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual LeaveCompanyResult LeaveCompany(LeaveCompanyArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            var sessionInfo = SessionInfoService.Get(AccessToken)
                ?? throw new UnauthorizedAccessException("Session not found or has expired.");

            if (sessionInfo.CompanyId != null)
            {
                sessionInfo.CompanyId = null;
                SessionInfoService.Set(sessionInfo);
            }

            return new LeaveCompanyResult();
        }

        /// <summary>
        /// Destroys the current session, clearing any company context first.
        /// </summary>
        /// <param name="args">The input arguments (currently carries no fields).</param>
        /// <remarks>
        /// Idempotent — calling on an unknown or already-expired access token succeeds
        /// without error. The clean-up sequence is: clear <c>SessionInfo.CompanyId</c>
        /// (no-op if already null), then remove the session entry from the cache. Callers
        /// do not need to call <c>LeaveCompany</c> before <c>Logout</c>.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual LogoutResult Logout(LogoutArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            // Get-and-clear company context first so any consumer holding a SessionInfo
            // reference sees the clean state before the cache entry disappears.
            var sessionInfo = SessionInfoService.Get(AccessToken);
            if (sessionInfo != null && sessionInfo.CompanyId != null)
            {
                sessionInfo.CompanyId = null;
                SessionInfoService.Set(sessionInfo);
            }

            SessionInfoService.Remove(AccessToken);
            return new LogoutResult();
        }

        /// <summary>
        /// Validates the user's credentials.
        /// </summary>
        /// <param name="args">The login arguments.</param>
        /// <param name="userName">The user name on successful authentication.</param>
        /// <returns>True if authentication succeeded; otherwise, false.</returns>
        /// <remarks>
        /// The default implementation always returns <c>false</c> to prevent unauthorized access
        /// if a subclass forgets to override this method. Override in subclasses to implement real validation.
        /// </remarks>
        protected virtual bool AuthenticateUser(LoginArgs args, out string userName)
        {
            userName = string.Empty;
            return false;
        }

        private const int MaxExpiresInSeconds = 86400; // 24 hours

        /// <summary>
        /// Creates a new user session.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual CreateSessionResult CreateSession(CreateSessionArgs args)
        {
            if (args.ExpiresIn <= 0 || args.ExpiresIn > MaxExpiresInSeconds)
                throw new ArgumentOutOfRangeException(nameof(args),
                    $"args.ExpiresIn must be between 1 and {MaxExpiresInSeconds} seconds.");

            // Create a new user session
            var repo = Services.GetRequiredService<ISystemRepositoryFactory>().CreateSessionRepository();
            var user = repo.CreateSession(args.UserID, args.ExpiresIn, args.OneTime);
            // Return the access token
            return new CreateSessionResult()
            {
                AccessToken = user.AccessToken,
                ExpiredAt = user.EndTime
            };
        }

        /// <summary>
        /// Core method for retrieving definition data.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        private GetDefineResult GetDefineCore(GetDefineArgs args)
        {
            var result = new GetDefineResult();
            object value = DefineAccess.GetDefine(args.DefineType, args.Keys);

            if (value != null)
            {
                // If the definition implements ISerializableClone, create a copy first
                // to avoid polluting the cache during serialization
                if (value is ISerializableClone cloneable)
                {
                    value = cloneable.CreateSerializableCopy();
                }
                // Serialize the object to XML
                result.Xml = XmlCodec.Serialize(value);
            }

            return result;
        }

        /// <summary>
        /// Gets definition data (public). Sensitive definitions such as SystemSettings and DatabaseSettings are excluded.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetDefineResult GetDefine(GetDefineArgs args)
        {
            // Non-local calls are not permitted to access SystemSettings or DatabaseSettings
            if ((args.DefineType == DefineType.SystemSettings || args.DefineType == DefineType.DatabaseSettings) && !IsLocalCall)
                throw new NotSupportedException("The specified DefineType is not supported.");
            return GetDefineCore(args);
        }

        /// <summary>
        /// Returns a <see cref="FormSchema"/> as a typed object, intended for JS /
        /// TypeScript frontends that prefer JSON over the XML envelope returned by
        /// <see cref="GetDefine"/>. The Plain wire format serialises the schema as
        /// a JSON tree directly; the .NET client may keep using <see cref="GetDefine"/>.
        /// </summary>
        /// <param name="args">The input arguments carrying the target <c>ProgId</c>.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetFormSchemaResult GetFormSchema(GetFormSchemaArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.ProgId))
                throw new ArgumentException("ProgId is required.", nameof(args));

            var schema = LoadAndLocalizeSchema(args.ProgId);
            return new GetFormSchemaResult { Schema = schema };
        }

        /// <summary>
        /// Returns a <see cref="FormLayout"/> for the specified <c>ProgId</c> and
        /// optional <c>LayoutId</c>. The layout is generated on demand from the
        /// underlying <see cref="FormSchema"/>; for JS / TypeScript frontends the
        /// Plain wire format serialises it as a JSON tree ready for direct UI
        /// rendering.
        /// </summary>
        /// <param name="args">
        /// The input arguments. <c>ProgId</c> is required; <c>LayoutId</c> may be
        /// empty (defaults to <c>"default"</c> server-side).
        /// </param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetFormLayoutResult GetFormLayout(GetFormLayoutArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.ProgId))
                throw new ArgumentException("ProgId is required.", nameof(args));

            // Localize the schema first so the generated layout inherits localized
            // DisplayName / Caption values rather than the raw fixture text.
            var schema = LoadAndLocalizeSchema(args.ProgId);

            var layoutId = string.IsNullOrWhiteSpace(args.LayoutId) ? "default" : args.LayoutId;
            var layout = schema.GetFormLayout(layoutId);

            return new GetFormLayoutResult { Layout = layout };
        }

        /// <summary>
        /// Returns a <see cref="LanguageResource"/> as a typed object — JS / TypeScript
        /// frontends consume the result through the Plain JSON wire format.
        /// </summary>
        /// <remarks>
        /// <para>
        /// **JS-only API.** The <see cref="LanguageResource"/> family uses
        /// <c>KeyCollectionBase</c> internals that do not round-trip through
        /// MessagePack (the Encoded / Encrypted wire formats); the Plain JSON wire
        /// path used by JS / TypeScript clients works correctly. Sibling methods
        /// <see cref="GetFormSchema"/> and <see cref="GetFormLayout"/> follow the
        /// same convention. .NET clients should use <see cref="GetDefine"/> with
        /// <c>DefineType.Language</c> for the XML-based access path.
        /// </para>
        /// <para>
        /// The resource is read from the Define cache via
        /// <c>IDefineAccess.GetLanguage</c> and returned as-is. Per
        /// <c>docs/development-constraints.md § Definition Data Immutability After Init</c>,
        /// the cached instance must not be mutated; callers that need per-session
        /// adjustments should clone the result.
        /// </para>
        /// </remarks>
        /// <param name="args">The input arguments carrying <c>Lang</c> and <c>Namespace</c>.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetLanguageResult GetLanguage(GetLanguageArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.Lang))
                throw new ArgumentException("Lang is required.", nameof(args));
            if (string.IsNullOrWhiteSpace(args.Namespace))
                throw new ArgumentException("Namespace is required.", nameof(args));

            // GetLanguage returns null when the resource file does not exist;
            // that is a normal scenario (missing translation), not an error.
            var resource = DefineAccess.GetLanguage(args.Lang, args.Namespace);
            return new GetLanguageResult { Resource = resource };
        }

        /// <summary>
        /// Loads the <see cref="FormSchema"/> from the Define cache, deep-clones it via
        /// <see cref="FormSchema.Clone"/>, and applies localized text using the current
        /// session's <c>Culture</c>. The cloned instance is safe to mutate without
        /// affecting the shared cached schema.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The cached <see cref="FormSchema"/> is process-shared (every session reads
        /// the same in-memory instance) — see <c>docs/development-constraints.md</c>
        /// § <i>Definition Data Immutability After Init</i>. We must <b>not</b> mutate
        /// it, and we must <b>not</b> use <see cref="XmlCodec.Serialize(object)"/> as
        /// a deep-clone shortcut either: the serialization lifecycle flips
        /// <c>SerializeState</c> on the source, which races under concurrent load.
        /// </para>
        /// <para>
        /// <see cref="FormSchema.Clone"/> is a pure read of the source and produces a
        /// fully independent copy with no shared mutable state — safe under any number
        /// of concurrent callers in any combination of languages.
        /// </para>
        /// </remarks>
        /// <param name="progId">The program identifier.</param>
        private FormSchema LoadAndLocalizeSchema(string progId)
        {
            var raw = DefineAccess.GetDefine(DefineType.FormSchema, new[] { progId }) as FormSchema
                ?? throw new InvalidOperationException($"FormSchema '{progId}' not found.");

            // Skip the clone + localize round-trip when the caller has no session lang
            // (anonymous flows shouldn't be paying for a deep clone).
            string lang = GetCurrentLang();
            if (string.IsNullOrWhiteSpace(lang))
                return raw;

            var clone = raw.Clone();
            new FormSchemaLocalizer(LanguageService).Localize(clone, lang);
            return clone;
        }

        /// <summary>
        /// Core method for saving definition data.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        private SaveDefineResult SaveDefineCore(SaveDefineArgs args)
        {
            // Deserialize XML to the target object
            var type = args.DefineType.ToClrType();
            object? defineObject = XmlCodec.Deserialize(args.Xml, type);
            if (defineObject == null)
                throw new InvalidOperationException($"Failed to deserialize XML to {type.Name} object.");

            // Save the definition data
            DefineAccess.SaveDefine(args.DefineType, defineObject, args.Keys);
            var result = new SaveDefineResult();
            return result;
        }

        /// <summary>
        /// Saves definition data (public). Sensitive definitions such as SystemSettings and DatabaseSettings are excluded.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual SaveDefineResult SaveDefine(SaveDefineArgs args)
        {
            // Non-local calls are not permitted to save SystemSettings or DatabaseSettings
            if ((args.DefineType == DefineType.SystemSettings || args.DefineType == DefineType.DatabaseSettings) && !IsLocalCall)
                throw new NotSupportedException("The specified DefineType is not supported.");

            return SaveDefineCore(args);
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
