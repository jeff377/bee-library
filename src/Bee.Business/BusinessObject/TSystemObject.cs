using System;
using System.Reflection;
using Bee.Base;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 系統層級商業邏輯物件。
    /// </summary>
    public class TSystemObject : TBaseBusinessObject, ISystemObject
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TSystemObject() : base()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TSystemObject(Guid accessToken) : base(accessToken)
        { }

        #endregion

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

        /// <summary>
        /// 建立連線。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public TCreateSessionResult CreateSession(TCreateSessionArgs args)
        {
            // 建立一組用戶連線
            var helper = new TSessionDbHelper();
            var user = helper.Create(args.UserID, args.ExpiresIn, args.OneTime);
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
        public TGetDefineResult GetDefine(TGetDefineArgs args)
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
        public TSaveDefineResult SaveDefine(TSaveDefineArgs args)
        {
            TSaveDefineResult oResult;
            TCacheDefineAccess oAccess;

            oAccess = new TCacheDefineAccess();
            oAccess.SaveDefine(args.DefineType, args.DefineObject, args.Keys);
            oResult = new TSaveDefineResult();
            return oResult;
        }
    }
}
