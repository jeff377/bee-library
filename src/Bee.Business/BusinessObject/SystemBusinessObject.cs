using System;
using Bee.Base;
using Bee.Cache;
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
        public SystemBusinessObject() : base()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public SystemBusinessObject(Guid accessToken) : base(accessToken)
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
            string encryptedKey = RsaCryptor.EncryptWithPublicKey(
                Convert.ToBase64String(encryptionKey),
                args.ClientPublicKey
            );

            return new LoginResult
            {
                AccessToken = sessionInfo.AccessToken,
                ExpiredAt = sessionInfo.ExpiredAt,
                ApiEncryptionKey = encryptedKey,
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
        /// 取得定義資料。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Encrypted)]
        public virtual GetDefineResult GetDefine(GetDefineArgs args)
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
        /// 儲存定義資料。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(ApiProtectionLevel.Encrypted)]
        public virtual SaveDefineResult SaveDefine(SaveDefineArgs args)
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
        /// 執行 ExecFunc 方法的實作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            InvokeExecFunc(args, result);
        }

        /// <summary>
        /// 使用反射，執行 ExecFunc 方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        private void InvokeExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            try
            {
                // 使用反射，執行 FuncID 對應的自訂方法
                var execFunc = new SystemBusinessExecFunc(this.AccessToken);
                var method = execFunc.GetType().GetMethod(args.FuncID);
                if (method == null)
                    throw new MissingMethodException($"Method {args.FuncID} not found.");
                method.Invoke(execFunc, new object[] { args, result });
            }
            catch (Exception ex)
            {
                // 使用反射時，需抓取 InnerException 才是原始例外錯誤
                if (ex.InnerException != null)
                    throw ex.InnerException;
                else
                    throw ex;
            }
        }
    }
}
