using System;
using Bee.Base;
using Bee.Cache;
using Bee.Contracts;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 系統層級業務邏輯物件。
    /// </summary>
    public class SystemBusinessObject : BusinessObject, ISystemBusinessObject
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        public SystemBusinessObject(Guid accessToken, bool isLocalCall = true)
            : base(accessToken, isLocalCall)
        { }

        #endregion

        /// <summary>
        /// Ping 方法，測試 API 服務是否可用。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual PingResult Ping(PingArgs args)
        {
            return new PingResult()
            {
                Status = "ok",
                ServerTime = DateTime.UtcNow,
                Version = SysInfo.Version, // 系統版本
                TraceId = args.TraceId // 回傳追蹤 ID
            };
        }

        /// <summary>
        /// 取得通用參數及環境設置。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual GetCommonConfigurationResult GetCommonConfiguration(GetCommonConfigurationArgs args)
        {
            var commonConfiguration = CacheFunc.GetSystemSettings().CommonConfiguration;
            return new GetCommonConfigurationResult()
            {
                CommonConfiguration = commonConfiguration.ToXml()
            };
        }

        /// <summary>
        /// 執行登入操作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual LoginResult Login(LoginArgs args)
        {
            // 1. 驗證帳密並取得用戶名稱
            if (!AuthenticateUser(args, out var userName))
                throw new UnauthorizedAccessException("Invalid username or password.");

            // 2. 登入時產生一組金鑰（可能是共用或隨機金鑰）
            byte[] encryptionKey = BackendInfo.ApiEncryptionKeyProvider.GenerateKeyForLogin();

            // 3. 建立 SessionInfo 並存入快取
            var sessionInfo = new SessionInfo
            {
                AccessToken = Guid.NewGuid(),
                UserId = args.UserId,
                UserName = userName,
                ExpiredAt = DateTime.UtcNow.AddHours(1),
                ApiEncryptionKey = encryptionKey
            };
            CacheFunc.SetSessionInfo(sessionInfo);

            // 4. 回傳加密後的金鑰與 Token
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
        /// 驗證使用者帳號與密碼是否正確。
        /// </summary>
        /// <param name="args">登入引數。</param>
        /// <param name="userName">驗證成功的使用者名稱。</param>
        /// <returns>是否驗證成功。</returns>
        protected virtual bool AuthenticateUser(LoginArgs args, out string userName)
        {
            userName = "Demo User";
            return true; // 預設為通過，可由子類實作實際驗證邏輯
        }

        /// <summary>
        /// 建立連線。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
        public virtual CreateSessionResult CreateSession(CreateSessionArgs args)
        {
            // 建立一組用戶連線
            var repo = BackendInfo.RepositoryProvider.SessionRepository;
            var user = repo.CreateSession(args.UserID, args.ExpiresIn, args.OneTime);
            // 回傳存取令牌
            return new CreateSessionResult()
            {
                AccessToken = user.AccessToken,
                ExpiredAt = user.EndTime
            };
        }

        /// <summary>
        /// 取得定義資料的核心方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        private GetDefineResult GetDefineCore(GetDefineArgs args)
        {
            var result = new GetDefineResult();
            var access = new CacheDefineAccess();
            object value = access.GetDefine(args.DefineType, args.Keys);

            if (value != null)
            {
                // 如果定義資料實作 ISerializableClone，則先建立一份副本
                // 以避免在序列化過程中污染快取
                if (value is ISerializableClone cloneable)
                {
                    value = cloneable.CreateSerializableCopy();
                }
                // 將物件序列化為 XML
                result.Xml = SerializeFunc.ObjectToXml(value);
            }

            return result;
        }

        /// <summary>
        /// 取得定義資料（對外公開）。會排除機敏定義（例如：SystemSettings、DatabaseSettings）。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetDefineResult GetDefine(GetDefineArgs args)
        {
            // 非近端呼叫，禁止取得 SystemSettings 與 DatabaseSettings
            if (args.DefineType == DefineType.SystemSettings || args.DefineType == DefineType.DatabaseSettings)
            {
                if (!IsLocalCall) throw new NotSupportedException("The specified DefineType is not supported.");
            }
            return GetDefineCore(args);
        }

        /// <summary>
        /// 儲存定義資料的核心方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        private SaveDefineResult SaveDefineCore(SaveDefineArgs args)
        {
            // 將 XML 轉換為物件
            Type type = DefineFunc.GetDefineType(args.DefineType);
            object defineObject = SerializeFunc.XmlToObject(args.Xml, type);
            if (defineObject == null)
                throw new InvalidOperationException($"Failed to deserialize XML to {type.Name} object.");

            // 儲存定義資料
            var access = new CacheDefineAccess();
            access.SaveDefine(args.DefineType, defineObject, args.Keys);
            var result = new SaveDefineResult();
            return result;
        }

        /// <summary>
        /// 儲存定義資料（對外公開）。會排除機敏定義（例如：SystemSettings、DatabaseSettings）。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual SaveDefineResult SaveDefine(SaveDefineArgs args)
        {
            // 非近端呼叫，禁止儲存 SystemSettings 與 DatabaseSettings
            if (args.DefineType == DefineType.SystemSettings || args.DefineType == DefineType.DatabaseSettings)
            {
                if (!IsLocalCall) throw new NotSupportedException("The specified DefineType is not supported.");
            }

            return SaveDefineCore(args);
        }

        /// <summary>
        /// 檢查套件是否有更新版本。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Encoded, ApiAccessRequirement.Anonymous)]
        public virtual CheckPackageUpdateResult CheckPackageUpdate(CheckPackageUpdateArgs args)
        {
            // Implemented in derived classes.
            throw new NotSupportedException("CheckPackageUpdate is not implemented in the base class.");
        }

        /// <summary>
        /// 取得套件資訊。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Encoded, ApiAccessRequirement.Anonymous)]
        public virtual GetPackageResult GetPackage(GetPackageArgs args)
        {
            // Implemented in derived classes.
            throw new NotSupportedException("GetPackage is not implemented in the base class.");
        }

        /// <summary>
        /// 執行 ExecFunc 方法的實作。
        /// </summary>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new SystemExecFuncHandler(AccessToken);
            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result);
        }

        /// <summary>
        /// 執行 ExecFuncAnonymou 方法的實作。
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new SystemExecFuncHandler(AccessToken);
            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Anonymous, args, result);
        }
    }
}
