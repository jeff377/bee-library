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
        /// <param name="request">JSON-RPC 請求模型。</param>
        public TJsonRpcResponse Execute(TJsonRpcRequest request)
        {
            var response = new TJsonRpcResponse(request);
            try
            {
                // 傳輸資料是否加密
                bool encrypted = request.Encrypted;
                // 傳入參數進行解密
                if (encrypted)
                    request.Decrypt();

                // 建立商業邏輯物件，執行指定方法
                var businessObject = CreateBusinessObject(request);
                var method = businessObject.GetType().GetMethod(request.Action);
                var value = method.Invoke(businessObject, new object[] { request.Value });

                // 傳出結果
                response.Value = value;
                // 傳出結果進行加密
                if (encrypted)
                    response.Encrypt();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    response.Message = ex.InnerException.Message;
                else
                    response.Message = ex.Message;
            }
            return response;
        }

        /// <summary>
        /// 建立商業邏輯物件。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        private object CreateBusinessObject(TJsonRpcRequest request)
        {
            if (StrFunc.IsEmpty(request.ProgID))
                throw new TException("ProgID is empty");

            if (StrFunc.IsEquals(request.ProgID, SysProgIDs.System))
                return BackendInfo.BusinessObjectProvider.CreateSystemObject(AccessToken);
            else
                return BackendInfo.BusinessObjectProvider.CreateBusinessObject(AccessToken, request.ProgID);
        }
    }
}
