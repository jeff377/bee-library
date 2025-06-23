using System;
using Bee.Base;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 系統層級業務邏輯物件。
    /// </summary>
    public class TSystemBusinessObject : TBusinessObject, ISystemBusinessObject
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TSystemBusinessObject() : base()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TSystemBusinessObject(Guid accessToken) : base(accessToken)
        { }

        #endregion

        /// <summary>
        /// Ping 方法，測試 API 服務是否可用。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public virtual TPingResult Ping(TPingArgs args)
        {
            return new TPingResult()
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
        public virtual TGetApiPayloadOptionsResult GetApiPayloadOptions(TGetApiPayloadOptionsArgs args)
        {
            var options = CacheFunc.GetSystemSettings().CommonConfiguration.ApiPayloadOptions;
            return new TGetApiPayloadOptionsResult()
            {
                Serializer = options.Serializer,
                Compressor = options.Compressor,
                Encryptor = options.Encryptor
            };
        }

        /// <summary>
        /// 建立連線。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public virtual TCreateSessionResult CreateSession(TCreateSessionArgs args)
        {
            // 建立一組用戶連線
            var repo = BackendInfo.RepositoryProvider.SessionRepository;
            var user = repo.CreateSession(args.UserID, args.ExpiresIn, args.OneTime);
            // 回傳存取令牌
            return new TCreateSessionResult()
            {
                AccessToken = user.AccessToken,
                Expires = user.EndTime
            };
        }

        /// <summary>
        /// 取得定義資料。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(EApiProtectionLevel.Internal)]
        public virtual TGetDefineResult GetDefine(TGetDefineArgs args)
        {
            var result = new TGetDefineResult();
            var access = new TCacheDefineAccess();
            object value = access.GetDefine(args.DefineType, args.Keys);
            if (value != null)
                result.Xml = SerializeFunc.ObjectToXml(value);
            return result;
        }

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        [ApiAccessControl(EApiProtectionLevel.Internal)]
        public virtual TSaveDefineResult SaveDefine(TSaveDefineArgs args)
        {
            // 將 XML 轉換為物件
            Type type = DefineFunc.GetDefineType(args.DefineType);
            object defineObject = SerializeFunc.XmlToObject(args.Xml, type);
            if (defineObject == null)
                throw new InvalidOperationException($"Failed to deserialize XML to {type.Name} object.");

            // 儲存定義資料
            var access = new TCacheDefineAccess();
            access.SaveDefine(args.DefineType, defineObject, args.Keys);
            var result = new TSaveDefineResult();
            return result;
        }

        /// <summary>
        /// 執行 ExecFunc 方法的實作。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        protected override void DoExecFunc(TExecFuncArgs args, TExecFuncResult result)
        {
            InvokeExecFunc(args, result);
        }

        /// <summary>
        /// 使用反射，執行 ExecFunc 方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        /// <param name="result">傳出結果。</param>
        private void InvokeExecFunc(TExecFuncArgs args, TExecFuncResult result)
        {
            try
            {
                // 使用反射，執行 FuncID 對應的自訂方法
                var execFunc = new TSystemExecFunc(this.AccessToken);
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
