using System;
using System.Reflection;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// API 服務執行器。
    /// </summary>
    public class TApiServiceExecutor
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public TApiServiceExecutor(Guid accessToken)
        {
            AccessToken = accessToken;
        }

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 執行 API 服務。
        /// </summary>
        /// <param name="args">傳入參數。</param>
        public TApiServiceResult Execute(TApiServiceArgs args)
        {
            TApiServiceResult oResult;
            MethodInfo oMethod;
            object oBO;
            object oValue;
            bool bEncrypt;

            oResult = new TApiServiceResult(args);
            try
            {
                // 傳輸資料是否加密
                bEncrypt = args.Encrypted;
                // 傳入參數進行解密
                if (bEncrypt)
                    args.Decrypt();

                // 建立商業邏輯物件，執行指定方法
                oBO = CreateBO(args);
                oMethod = oBO.GetType().GetMethod(args.Action);
                oValue = oMethod.Invoke(oBO, new object[] { args.Value });

                // 傳出結果
                oResult.Value = oValue;
                // 傳出結果進行加密
                if (bEncrypt)
                    oResult.Encrypt();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    oResult.Message = ex.InnerException.Message;
                else
                    oResult.Message = ex.Message;
            }
            return oResult;
        }

        /// <summary>
        /// 建立商業邏輯物件。
        /// </summary>
        /// <param name="args">傳入參數。</param>
        private object CreateBO(TApiServiceArgs args)
        {
            if (StrFunc.IsEmpty(args.ProgID))
                throw new TException("ProgID is empty");

            if (StrFunc.IsEquals(args.ProgID, SysProgIDs.System))
                return BackendInfo.BusinessObjectProvider.CreateSystemObject(AccessToken);
            else
                return BackendInfo.BusinessObjectProvider.CreateBusinessObject(AccessToken, args.ProgID);
        }
    }
}
