using Bee.Definition.Settings;
using System;
using Bee.Base;
using Bee.Base.Security;
using Bee.Base.Serialization;
using Bee.Business.System;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Repository.Abstractions;

namespace Bee.Business.BusinessObjects
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
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public SystemBusinessObject(Guid accessToken, bool isLocalCall = true)
            : base(accessToken, isLocalCall)
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
            var settings = BackendInfo.DefineAccess.GetSystemSettings();
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
            var tracker = BackendInfo.LoginAttemptTracker;

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
            byte[] encryptionKey = BackendInfo.ApiEncryptionKeyProvider.GenerateKeyForLogin();

            // 3. Create SessionInfo and store it in the cache
            var sessionInfo = new SessionInfo
            {
                AccessToken = Guid.NewGuid(),
                UserId = args.UserId,
                UserName = userName,
                ExpiredAt = DateTime.UtcNow.AddHours(1),
                ApiEncryptionKey = encryptionKey
            };
            BackendInfo.SessionInfoService.Set(sessionInfo);

            // 4. Return the encrypted key and access token
            string encryptedKey = string.Empty;
            if (StrFunc.IsNotEmpty(args.ClientPublicKey))
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
                throw new ArgumentOutOfRangeException(nameof(args.ExpiresIn),
                    $"ExpiresIn must be between 1 and {MaxExpiresInSeconds} seconds.");

            // Create a new user session
            var repo = RepositoryInfo.SystemProvider!.SessionRepository;
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
        private static GetDefineResult GetDefineCore(GetDefineArgs args)
        {
            var result = new GetDefineResult();
            var access = BackendInfo.DefineAccess;
            object value = access.GetDefine(args.DefineType, args.Keys);

            if (value != null)
            {
                // If the definition implements ISerializableClone, create a copy first
                // to avoid polluting the cache during serialization
                if (value is ISerializableClone cloneable)
                {
                    value = cloneable.CreateSerializableCopy();
                }
                // Serialize the object to XML
                result.Xml = SerializeFunc.ObjectToXml(value);
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
        /// Core method for saving definition data.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        private static SaveDefineResult SaveDefineCore(SaveDefineArgs args)
        {
            // Deserialize XML to the target object
            var type = DefineFunc.GetDefineType(args.DefineType);
            object? defineObject = SerializeFunc.XmlToObject(args.Xml, type);
            if (defineObject == null)
                throw new InvalidOperationException($"Failed to deserialize XML to {type.Name} object.");

            // Save the definition data
            var access = BackendInfo.DefineAccess;
            access.SaveDefine(args.DefineType, defineObject, args.Keys);
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
            var handler = new SystemExecFuncHandler(AccessToken);
            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result);
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFuncAnonymous"/>.
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new SystemExecFuncHandler(AccessToken);
            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Anonymous, args, result);
        }
    }
}
