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
        /// <param name="format">傳輸資料的封裝格式。</param>
        protected T Execute<T>(string progId, string action, object value, PayloadFormat format)
        {
            if (StrFunc.IsEmpty(progId))
                throw new ArgumentException("progId cannot be null or empty.", nameof(progId));
            if (StrFunc.IsEmpty(action))
                throw new ArgumentException("action cannot be null or empty.", nameof(action));

            // 建立 JSON-RPC 請求模型
            var request = CreateRequest(progId, action, value);
            LogRawData(request);

            // 將參數格式轉換為指定的 payloadFormat
            var actualFormat = TransformRequestPayload(request, format);
            // 呼叫遠端或近端 JSON-RPC 方法
            var response = this.Provider.Execute(request);
            LogRawData(response);

            if (response.Error != null)
                throw new InvalidOperationException($"API error: {response.Error.Code} - {response.Error.Message}");

            // 還原回應資料（若為 Encoded 或 Encrypted）
            RestoreResponsePayload(response, actualFormat);

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
        /// 將指定的 JSON-RPC 請求物件轉換為目標傳輸格式（Plain、Encoded 或 Encrypted）。
        /// </summary>
        /// <param name="request">要處理的 JSON-RPC 請求物件。</param>
        /// <param name="format">
        /// 欲套用的資料格式：
        /// <list type="bullet">
        /// <item><description><see cref="PayloadFormat.Plain"/>：不做處理。</description></item>
        /// <item><description><see cref="PayloadFormat.Encoded"/>：執行序列化與壓縮。</description></item>
        /// <item><description><see cref="PayloadFormat.Encrypted"/>：執行序列化、壓縮並加密。</description></item>
        /// </list>
        /// </param>
        /// <returns>實際執行後的資料格式（根據執行環境可能會降級為 Plain）。</returns>
        private PayloadFormat TransformRequestPayload(JsonRpcRequest request, PayloadFormat format)
        {
            if (this.Provider is LocalApiServiceProvider && !SysInfo.IsDebugMode)
            {
                format = PayloadFormat.Plain; // 本地非除錯模式下不進行編碼
            }

            if (format != PayloadFormat.Plain)
            {
                ApiPayloadConverter.TransformTo(request.Params, format, FrontendInfo.ApiEncryptionKey);
                LogEncodedData(request);
            }

            return format;
        }

        /// <summary>
        /// 還原 JSON-RPC 回應資料內容，依據指定格式解碼與解密回原始物件。
        /// </summary>
        /// <param name="response">要還原的 JSON-RPC 回應物件。</param>
        /// <param name="format">
        /// 回應資料的格式：
        /// <list type="bullet">
        /// <item><description><see cref="PayloadFormat.Plain"/>：不處理，直接使用。</description></item>
        /// <item><description><see cref="PayloadFormat.Encoded"/> 或 <see cref="PayloadFormat.Encrypted"/>：進行解碼或解密處理。</description></item>
        /// </list>
        /// </param>
        private void RestoreResponsePayload(JsonRpcResponse response, PayloadFormat format)
        {
            if (format == PayloadFormat.Plain)
                return;

            ApiPayloadConverter.RestoreFrom(response.Result, format, FrontendInfo.ApiEncryptionKey);
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
            LogWrite($"Raw Data:{Environment.NewLine}{json}");
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
            LogWrite($"Encoded Data:{Environment.NewLine}{json}");
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
