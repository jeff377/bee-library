using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Api.Core
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
        public TJsonRpcResponse Execute(TJsonRpcRequest args)
        {
            var result = new TJsonRpcResponse(args);
            try
            {
                // 傳輸資料是否加密
                bool encrypted = args.Encrypted;
                // 傳入參數進行解密
                if (encrypted)
                    args.Decrypt();

                // 建立商業邏輯物件，執行指定方法
                var businessObject = CreateBusinessObject(args);
                var method = businessObject.GetType().GetMethod(args.Action);
                var value = method.Invoke(businessObject, new object[] { args.Value });

                // 傳出結果
                result.Value = value;
                // 傳出結果進行加密
                if (encrypted)
                    result.Encrypt();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    result.Message = ex.InnerException.Message;
                else
                    result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// 建立商業邏輯物件。
        /// </summary>
        /// <param name="args">傳入參數。</param>
        private object CreateBusinessObject(TJsonRpcRequest args)
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
