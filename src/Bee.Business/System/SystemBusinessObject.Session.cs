using Bee.Base;
using Bee.Base.Security;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
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
            WriteLoginAudit(LoginEvent.LoginSucceeded, sessionInfo.UserId, sessionInfo.UserName, sessionInfo.AccessToken, null, LoginSource);

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
            // Derive the session's customization code from the company (empty when the company
            // ships no customization). The session-level overlay reads this value downstream.
            sessionInfo.CustomizeId = companyInfo.CustomizeId;
            // Snapshot the user's roles for this company so the layer-1 Can check runs from
            // SessionInfo.Roles (sys_id) without re-hitting the database on every request.
            var rolePermissionService = Services.GetRequiredService<IRolePermissionService>();
            var snapshot = rolePermissionService.Get(args.CompanyId);
            sessionInfo.Roles = snapshot?.GetUserRoleIds(sessionInfo.UserId).ToList() ?? [];
            // Compute the per-model capability snapshot once, here, where the roles and the
            // permission snapshot are both in hand — the client caches it to degrade UI elements
            // without any extra round-trip (see plan-permission-capability.md).
            var capabilities = snapshot?.GetAllowedByModel(sessionInfo.Roles) ?? [];
            // Snapshot the user's record-scope identity (user/employee/department row ids) so
            // layer-2 scope filtering runs from the session without re-hitting the database.
            var employeeResolver = Services.GetRequiredService<IEmployeeContextResolver>();
            var employeeContext = employeeResolver.Resolve(sessionInfo.UserId, companyInfo.CompanyDatabaseId);
            sessionInfo.UserRowId = employeeContext.UserRowId;
            sessionInfo.EmployeeRowId = employeeContext.EmployeeRowId;
            sessionInfo.DeptRowId = employeeContext.DeptRowId;
            SessionInfoService.Set(sessionInfo);

            return new EnterCompanyResult { Company = companyInfo, Capabilities = capabilities };
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
                ClearCompanyContext(sessionInfo);
                SessionInfoService.Set(sessionInfo);
            }

            WriteLoginAudit(LoginEvent.Logout, sessionInfo?.UserId, sessionInfo?.UserName, AccessToken, null, LogoutSource);
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
    }
}
