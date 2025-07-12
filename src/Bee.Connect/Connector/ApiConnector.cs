using System;
using Bee.Api.Core;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// API 服務連接器基底類別。
    /// </summary>
    public abstract class ApiConnector
    {
        #region 建構函式

        /// <summary>
        /// 建構函式，採用近端連線。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public ApiConnector(Guid accessToken)
        {
            AccessToken = accessToken;
            Provider = new LocalApiServiceProvider(accessToken);
        }

        /// <summary>
        /// 建構函式，採用遠端連線。
        /// </summary>
        /// <param name="endpoint">API 服務端點。</param>
        /// <param name="accessToken">存取令牌。</param>
        public ApiConnector(string endpoint, Guid accessToken)
        {
            if (StrFunc.IsEmpty(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));

            AccessToken = accessToken;
            Provider = new RemoteApiServiceProvider(endpoint, accessToken);
        }

        #endregion

        /// <summary>
        /// 存取令牌。
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// API 服務提供者。
        /// </summary>
        public IJsonRpcProvider Provider { get; private set; }

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="action">執行動作。</param>
        /// <param name="value">對應執行動作的傳入參數。</param>
        /// <param name="enableEncoding">是否啟用資料編碼（序列化、壓縮與加密）。</param>
        protected T Execute<T>(string progId, string action, object value, bool enableEncoding = true)
        {
            if (StrFunc.IsEmpty(progId))
                throw new ArgumentException("progId cannot be null or empty.", nameof(progId));
            if (StrFunc.IsEmpty(action))
                throw new ArgumentException("action cannot be null or empty.", nameof(action));

            // 建立 JSON-RPC 請求模型
            var request = CreateRequest(progId, action, value);
            // 記錄 JSON-RPC 請求的原始內容
            LogRawData(request);

            // 根據目前環境判斷是否進行請求資料編碼，並記錄編碼後內容
            enableEncoding = TryEncodeRequest(request, enableEncoding);

            // 執行 API 方法，並取得 JSON-RPC 回應
            var response = this.Provider.Execute(request);
            // 記錄 JSON-RPC 回應的原始內容
            LogRawData(response);

            // 若啟用資料編碼，則對回應資料進行解碼，並記錄編碼內容
            TryDecodeResponse(response, enableEncoding);

            if (response.Error != null)
                throw new InvalidOperationException($"API error: {response.Error.Code} - {response.Error.Message}");

            return (T)response.Result.Value;
        }

        /// <summary>
        /// 建立 JSON-RPC 請求物件。
        /// </summary>
        /// <param name="progId">功能程式代碼（如 Employee、Login 等）。</param>
        /// <param name="action">執行動作名稱（如 Hello、GetList）。</param>
        /// <param name="value">傳遞至伺服端的參數物件。</param>
        /// <returns>組成後的 JSON-RPC 請求物件。</returns>
        private JsonRpcRequest CreateRequest(string progId, string action, object value)
        {
            return new JsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new JsonRpcParams
                {
                    Value = value
                },
                Id = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// 根據目前環境判斷是否進行請求資料編碼，並記錄編碼後內容。
        /// </summary>
        /// <param name="request">JSON-RPC 請求物件。</param>
        /// <param name="enableEncoding">是否啟用編碼的旗標。</param>
        /// <returns>實際是否執行了編碼。</returns>
        private bool TryEncodeRequest(JsonRpcRequest request, bool enableEncoding)
        {
            if (this.Provider is LocalApiServiceProvider && !SysInfo.IsDebugMode)
            {
                enableEncoding = false;
            }

            if (enableEncoding)
            {
                request.Encode(FrontendInfo.ApiEncryptionKey);
                LogEncodedData(request);
            }

            return enableEncoding;
        }

        /// <summary>
        /// 若啟用資料編碼，則對回應資料進行解碼，並記錄編碼內容。
        /// </summary>
        /// <param name="response">JSON-RPC 回應物件。</param>
        /// <param name="enableEncoding">是否啟用資料解碼。</param>
        private void TryDecodeResponse(JsonRpcResponse response, bool enableEncoding)
        {
            if (!enableEncoding) return;

            response.Decode(FrontendInfo.ApiEncryptionKey);
            LogEncodedData(response);
        }

        /// <summary>
        /// 記錄 JSON-RPC 的原始資料內容。
        /// </summary>
        /// <param name="value">原始資料物件。</param>
        private void LogRawData(IObjectSerialize value)
        {
            if (value == null) { return; }
            if (!SysInfo.LogOptions.ApiConnector.RawData) { return; }

            string json = value.ToJson();   
            LogWrite($"Raw Data: {json}");
        }

        /// <summary>
        /// 記錄 JSON-RPC 的編碼後資料。
        /// </summary>
        /// <param name="value">編碼後資料物件。</param>
        private void LogEncodedData(IObjectSerialize value)
        {
            if (value == null) { return; }
            if (!SysInfo.LogOptions.ApiConnector.EncodedData) { return; }

            string json = value.ToJson();
            LogWrite($"Encoded Data: {json}");
        }

        /// <summary>
        /// 寫入日誌。
        /// </summary>
        /// <param name="message">要記錄的訊息內容。</param>
        private void LogWrite(string message)
        {
            var entry = new LogEntry()
            {
                Message = message,
                Source = nameof(ApiConnector),
                EntryType = LogEntryType.Information,
                Timestamp = DateTime.Now
            };
            SysInfo.LogWriter.Write(entry); 
        }

    }
}
