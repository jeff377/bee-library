using System;
using System.Security.Cryptography.X509Certificates;
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
        /// 取得 API 傳輸層的 Payload 編碼選項。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public virtual GetApiPayloadOptionsResult GetApiPayloadOptions(GetApiPayloadOptionsArgs args)
        {
            var options = CacheFunc.GetSystemSettings().CommonConfiguration.ApiPayloadOptions;
            return new GetApiPayloadOptionsResult()
            {
                Serializer = options.Serializer,
                Compressor = options.Compressor,
                Encryptor = options.Encryptor
            };
        }

        /// <summary>
        /// 登入系統。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public virtual LoginResult Login(LoginArgs args)
        {
            // 伺端端建立一組隨機 AES + HMAC 金鑰
            //string combinedKey = AesCbcHmacKeyGenerator.GenerateBase64CombinedKey();

            string apiEncryptionKey = Convert.ToBase64String(BackendInfo.ApiEncryptionKey);

            // 將伺服端建立的金鑰，使用公鑰加密回傳給用戶端
            string encryptedSessionKey = RsaCryptor.EncryptWithPublicKey(apiEncryptionKey, args.ClientPublicKey);

            return new LoginResult()
            {
                AccessToken = Guid.NewGuid(),
                ExpiredAt = DateTime.UtcNow.AddHours(1), // 預設為 1 小時後過期
                EncryptedSessionKey = encryptedSessionKey
            };
        }

        /// <summary>
        /// 建立連線。
        /// </summary>
        /// <param name="args">傳入引數。</param>
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
                result.Xml = SerializeFunc.ObjectToXml(value);
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
                var execFunc = new SystemExecFunc(this.AccessToken);
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
