using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Base.Security;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Bee.Business.Session;
using Bee.Definition.Identity;
using Bee.Definition.Security;

namespace Bee.Business.System
{
    /// <summary>
    /// Session / authentication half of <see cref="SystemBusinessObject"/> (login, company context,
    /// logout and session creation). Split out for file size only; behaviour is unchanged.
    /// </summary>
    public partial class SystemBusinessObject
    {
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
            {
                WriteLoginAudit(LoginEvent.LockedOut, args.UserId, null, null, "Account temporarily locked.", LoginSource);
                throw new UnauthorizedAccessException("Account is temporarily locked due to too many failed login attempts. Please try again later.");
            }

            // 1. Authenticate credentials and retrieve the user name
            if (!AuthenticateUser(args, out var userName))
            {
                tracker?.RecordFailure(args.UserId);
                WriteLoginAudit(LoginEvent.LoginFailed, args.UserId, null, null, "Invalid username or password.", LoginSource);
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            // Clear failed attempt history on successful login
            tracker?.Reset(args.UserId);

            // 2. Build the session: derive the key, persist the seed, then fill the cache.
            var sessionInfo = CreateSessionInfo(args.UserId, userName, s_defaultSessionLifetime);
            byte[] encryptionKey = sessionInfo.ApiEncryptionKey;
            WriteLoginAudit(LoginEvent.LoginSucceeded, sessionInfo.UserId, sessionInfo.UserName, sessionInfo.AccessToken, null, LoginSource);

            // 3. Return the encrypted key and access token
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
                TimeZone = sessionInfo.TimeZone,
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
        /// <see cref="CompanyAccessDeniedException"/> with the message
        /// <c>"Company access denied."</c> so callers cannot enumerate companies by
        /// probing the error text. Over JSON-RPC it arrives as
        /// <c>JsonRpcErrorCode.CompanyAccessDenied</c> (HTTP 403 semantics).
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual EnterCompanyResult EnterCompany(EnterCompanyArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (string.IsNullOrWhiteSpace(args.CompanyId))
                throw new ArgumentException("CompanyId is required.", nameof(args));

            var sessionInfo = SessionInfoService.Get(AccessToken)
                ?? throw new UnauthorizedAccessException("Session not found or has expired.");

            // The same binder runs on session rebuild, so entering a company and coming back from
            // an evicted cache land on identical session state.
            var binding = Services.GetRequiredService<SessionCompanyBinder>().Bind(sessionInfo, args.CompanyId)
                ?? throw new CompanyAccessDeniedException("Company access denied.");

            // Seed before cache: the company is the one snapshotted value that cannot be derived,
            // so a rebuild that missed it would silently drop the user back to "no company".
            SessionRepository.UpdateSession(CreateSeed(sessionInfo));
            SessionInfoService.Set(sessionInfo);

            return new EnterCompanyResult { Company = binding.Company, Capabilities = binding.Capabilities };
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
                ClearCompanyContext(sessionInfo);
                // Clearing only the cache would let a rebuild put the user back into the company
                // they just left.
                SessionRepository.UpdateSession(CreateSeed(sessionInfo));
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
        /// (no-op if already null), delete the seed from <c>st_session</c>, then remove the
        /// session entry from the cache. Callers do not need to call <c>LeaveCompany</c>
        /// before <c>Logout</c>.
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
                ClearCompanyContext(sessionInfo);
                SessionInfoService.Set(sessionInfo);
            }

            WriteLoginAudit(LoginEvent.Logout, sessionInfo?.UserId, sessionInfo?.UserName, AccessToken, null, LogoutSource);
            // WARNING: the seed must go before the cache entry, and it must go at all. Sessions are
            // rebuilt from `st_session`, so clearing only the cache would let the very next request
            // restore this token from its row — sign-out would do nothing. Deleting the row first
            // also means a failure here surfaces as a failed sign-out rather than a silent one.
            SessionRepository.DeleteSession(AccessToken);
            SessionInfoService.Remove(AccessToken);
            return new LogoutResult();
        }

        /// <summary>
        /// Clears every company-scoped value snapshotted onto the session by <c>EnterCompany</c>
        /// (company id, customization code, roles, and the record-scope identity row ids), leaving
        /// the session alive but company-less. Caller persists the change via
        /// <c>SessionInfoService.Set</c>.
        /// </summary>
        /// <param name="sessionInfo">The session to reset.</param>
        private static void ClearCompanyContext(SessionInfo sessionInfo)
        {
            sessionInfo.CompanyId = null;
            sessionInfo.CustomizeId = string.Empty;
            sessionInfo.Roles = [];
            sessionInfo.UserRowId = Guid.Empty;
            sessionInfo.EmployeeRowId = Guid.Empty;
            sessionInfo.DeptRowId = Guid.Empty;
        }

        private const string LoginSource = "System.Login";
        private const string LogoutSource = "System.Logout";
        private const string CreateSessionSource = "System.CreateSession";

        /// <summary>
        /// How long a session issued by <see cref="Login"/> stays valid.
        /// </summary>
        private static readonly TimeSpan s_defaultSessionLifetime = TimeSpan.FromHours(1);

        /// <summary>
        /// Builds a session for an already-identified user, persists its seed, and puts it in the
        /// cache. Shared by <see cref="Login"/> and <see cref="CreateSession"/>, which differ only
        /// in whether a credential check preceded them.
        /// </summary>
        /// <param name="userId">The authenticated user id.</param>
        /// <param name="userName">The user's display name.</param>
        /// <param name="lifetime">How long the session stays valid.</param>
        /// <returns>The session, already persisted and cached.</returns>
        /// <remarks>
        /// WARNING: the seed is written before the cache entry, and that order is not incidental.
        /// The two inconsistent states are not equally bad — a seed with no cache entry is rebuilt
        /// on the next request, while a cache entry with no seed is a token that dies at the next
        /// restart and does not exist at all on another node, which the client has no way to know.
        ///
        /// A failed seed write therefore fails the whole operation rather than being swallowed:
        /// handing back a token that is already doomed defers the fault to some later request,
        /// where it is far harder to diagnose.
        /// </remarks>
        private SessionInfo CreateSessionInfo(string userId, string userName, TimeSpan lifetime)
        {
            // The access token has to exist before the key: a deriving provider takes it as key
            // material, which is what makes the key recoverable when the session is rebuilt.
            var accessToken = Guid.NewGuid();
            byte[] encryptionKey = Services.GetRequiredService<IApiEncryptionKeyProvider>()
                .GenerateKeyForLogin(accessToken);

            var sessionInfo = new SessionInfo
            {
                AccessToken = accessToken,
                UserId = userId,
                UserName = userName,
                ExpiredAt = DateTime.UtcNow.Add(lifetime),
                ApiEncryptionKey = encryptionKey
            };
            ApplyUserLocale(sessionInfo);

            SessionRepository.InsertSession(CreateSeed(sessionInfo));
            SessionInfoService.Set(sessionInfo);
            return sessionInfo;
        }

        /// <summary>
        /// Gets the session seed repository.
        /// </summary>
        private ISessionRepository SessionRepository
            => Services.GetRequiredService<IRepositoryFactory>().Create<ISessionRepository>();

        /// <summary>
        /// Projects a session onto the seed persisted in <c>st_session</c>: the values that cannot
        /// be derived again. Everything else is recomputed when the session is rebuilt.
        /// </summary>
        /// <param name="sessionInfo">The live session.</param>
        private static SessionUser CreateSeed(SessionInfo sessionInfo)
        {
            return new SessionUser
            {
                AccessToken = sessionInfo.AccessToken,
                UserID = sessionInfo.UserId,
                UserName = sessionInfo.UserName,
                EndTime = sessionInfo.ExpiredAt,
                CompanyId = sessionInfo.CompanyId,
            };
        }

        /// <summary>
        /// Writes a login-axis audit entry when audit logging and its login category are both
        /// enabled. Resolved through the <see cref="IBeeContext.Services"/> escape hatch (same
        /// pattern as <see cref="ILoginAttemptTracker"/>); a no-op when disabled.
        /// </summary>
        private void WriteLoginAudit(LoginEvent loginEvent, string? userId, string? userName, Guid? accessToken, string? failReason, string source)
        {
            var options = Services.GetService<AuditLogOptions>();
            if (options is not { Enabled: true, LoginEnabled: true }) { return; }

            Services.GetService<IAuditLogWriter>()?.Write(new LoginAuditEntry
            {
                Event = loginEvent,
                UserId = userId,
                UserName = userName,
                AccessToken = accessToken,
                // Which application attempted the sign-in. This is the axis where it matters most:
                // a burst of failures from one application reads very differently from the same
                // burst spread across several.
                ApiKeyId = ApiKeyId,
                ApiKeyName = ApiKeyName,
                FailReason = failReason,
                Source = source,
            });
        }

        /// <summary>
        /// Validates the user's credentials.
        /// </summary>
        /// <param name="args">The login arguments.</param>
        /// <param name="userName">The user name on successful authentication.</param>
        /// <returns>True if authentication succeeded; otherwise, false.</returns>
        /// <remarks>
        /// The default implementation authenticates against the framework's own <c>st_user</c> table:
        /// the password is verified against the stored hash by <c>IUserRepository.VerifyPassword</c>
        /// and the display name is read from <c>st_user.sys_name</c>. Override in subclasses that
        /// authenticate against something else (a directory service, an identity provider, a
        /// deployment's own user table).
        /// <para>
        /// Comparing an account and a password is the same operation in every deployment, so it
        /// belongs to the framework rather than to each application: the table, the column and the
        /// hasher are all the framework's already.
        /// </para>
        /// <para>
        /// WARNING: an unknown user and a wrong password are indistinguishable to the caller by
        /// design. <c>Login</c> reports both as the same message so the endpoint cannot be used to
        /// enumerate accounts.
        /// </para>
        /// </remarks>
        protected virtual bool AuthenticateUser(LoginArgs args, out string userName)
        {
            userName = string.Empty;
            if (args == null || StringUtilities.IsEmpty(args.UserId)) { return false; }

            var repo = Services.GetRequiredService<IRepositoryFactory>().Create<IUserRepository>();
            if (!repo.VerifyPassword(args.UserId, args.Password ?? string.Empty)) { return false; }

            // A user with a blank name is a legitimate row, so the display name falls back to the
            // account id rather than leaving the session's UserName empty.
            var name = repo.GetName(args.UserId);
            userName = StringUtilities.IsNotEmpty(name) ? name! : args.UserId;
            return true;
        }

        /// <summary>
        /// Fills <see cref="SessionInfo.TimeZone"/> and <see cref="SessionInfo.Culture"/> from the
        /// user's <c>st_user</c> row, falling back to
        /// <see cref="BackendConfiguration.DefaultTimeZone"/> and
        /// <see cref="BackendConfiguration.DefaultLanguage"/> respectively.
        /// </summary>
        /// <param name="sessionInfo">The session being created.</param>
        /// <remarks>
        /// The session's zone is the authority for every user-facing date the framework produces —
        /// neither the device's zone nor the server machine's, so that filing a Taipei leave request
        /// from New York still defaults to the Taipei date (ADR-032 D12). The culture is the
        /// authority for every string the language service resolves for this session.
        ///
        /// An unset or unreadable user value falls back to the deployment-wide default rather than
        /// failing the login: authentication is overridable and a deployment may authenticate
        /// against something other than <c>st_user</c>, in which case there is no row to read.
        /// A deployment that wants UTC sets <c>DefaultTimeZone</c> to an empty string.
        ///
        /// Both values are read in a single query because both are needed on every login and live
        /// in the same row.
        /// </remarks>
        protected virtual void ApplyUserLocale(SessionInfo sessionInfo)
        {
            ArgumentNullException.ThrowIfNull(sessionInfo);

            var repo = Services.GetRequiredService<IRepositoryFactory>().Create<IUserRepository>();
            var locale = repo.GetLocale(sessionInfo.UserId);
            var backend = DefineAccess.GetSystemSettings().BackendConfiguration;
            sessionInfo.TimeZone = StringUtilities.IsNotEmpty(locale.TimeZone)
                ? locale.TimeZone
                : backend.DefaultTimeZone;
            sessionInfo.Culture = StringUtilities.IsNotEmpty(locale.Culture)
                ? locale.Culture
                : backend.DefaultLanguage;
        }

        private const int MaxExpiresInSeconds = 86400; // 24 hours

        /// <summary>
        /// Creates a new user session for the given user id. Restricted to local calls.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This issues an access token from a user id alone — it performs no credential check,
        /// which is what separates it from <see cref="Login"/>. That makes it a trusted-caller
        /// operation, so it is <see cref="ApiProtectionLevel.LocalOnly"/>: a remote caller able to
        /// reach it could mint a token for any account, including an administrator.
        /// </para>
        /// <para>
        /// It is the password-free counterpart of <see cref="Login"/>, for a background service
        /// that needs to run work through a business object on a user's behalf. It therefore takes
        /// exactly the same construction path — name resolution, locale, encryption key, seed,
        /// cache — minus the credential check, and the caller then calls <c>EnterCompany</c> like
        /// any other client. Before this, it wrote a bare row to <c>st_session</c> and nothing
        /// else, so the token it returned could not resolve a company and was unusable in practice.
        /// </para>
        /// </remarks>
        /// <param name="args">The input arguments.</param>
        /// <exception cref="NotSupportedException">
        /// Thrown when <c>args.OneTime</c> is set; see the remark below.
        /// </exception>
        [ApiAccessControl(ApiProtectionLevel.LocalOnly, ApiAccessRequirement.Anonymous)]
        public virtual CreateSessionResult CreateSession(CreateSessionArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (args.ExpiresIn <= 0 || args.ExpiresIn > MaxExpiresInSeconds)
                throw new ArgumentOutOfRangeException(nameof(args),
                    $"args.ExpiresIn must be between 1 and {MaxExpiresInSeconds} seconds.");
            // One-time semantics used to rest on delete-on-read in the seed repository. Now that a
            // session is cached at creation, the first use is a cache hit and the row is never
            // read, so the guarantee could not be honoured. Refusing outright is the only honest
            // option: silently degrading a security-flavoured promise to "ordinary session" is
            // worse than failing. A future handoff-token design would consume the token by
            // exchanging it for a real session, which is a genuine single consumption point.
            if (args.OneTime)
                throw new NotSupportedException(
                    "One-time sessions are no longer supported. Create an ordinary session and " +
                    "keep its lifetime short instead.");

            var userRepository = Services.GetRequiredService<IRepositoryFactory>().Create<IUserRepository>();
            // The message deliberately omits the user id: InvalidOperationException is on the
            // user-facing allow-list in JsonRpcExecutor, so its text reaches the caller verbatim
            // and would confirm whether an account exists.
            var userName = userRepository.GetName(args.UserID)
                ?? throw new InvalidOperationException("User not found.");

            var sessionInfo = CreateSessionInfo(args.UserID, userName, TimeSpan.FromSeconds(args.ExpiresIn));
            // Issuing a token for someone without their credentials is worth a trail of its own.
            WriteLoginAudit(LoginEvent.ServiceSessionCreated, sessionInfo.UserId, sessionInfo.UserName,
                sessionInfo.AccessToken, null, CreateSessionSource);

            return new CreateSessionResult()
            {
                AccessToken = sessionInfo.AccessToken,
                ExpiredAt = sessionInfo.ExpiredAt
            };
        }
    }
}
