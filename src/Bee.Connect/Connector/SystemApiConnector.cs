using System;
using Bee.Api.Core;
using Bee.Base;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// 系統層級 API 服務連接器。
    /// </summary>
    public class SystemApiConnector : ApiConnector
    {
        #region 建構函式

        /// <summary>
        /// 建構函式，採用近端連線。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public SystemApiConnector(Guid accessToken) : base(accessToken)
        { }

        /// <summary>
        /// 建構函式，採用遠端連線。
        /// </summary>
        /// <param name="endpoint">服務端點。。</param>
        /// <param name="accessToken">存取令牌。</param>
        public SystemApiConnector(string endpoint, Guid accessToken) : base(endpoint, accessToken)
        { }

        #endregion

        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="action">執行動作。</param>
        /// <param name="value">對應執行動作的傳入參數。</param>
        /// <param name="format">傳輸資料的封裝格式。</param>
        public T Execute<T>(string action, object value, PayloadFormat format = PayloadFormat.Encrypted)
        {
            return base.Execute<T>(SysProgIds.System, action, value, format);
        }

        /// <summary>
        /// 執行 Ping 方法，測試伺服端的連線狀態。
        /// </summary>
        public void Ping()
        {
            try
            {
                var args = new PingArgs()
                {
                    ClientName = "Connector",
                    TraceId = Guid.NewGuid().ToString()
                };
                var result = Execute<PingResult>(SystemActions.Ping, args, PayloadFormat.Plain);
                if (result.Status != "ok")
                    throw new InvalidOperationException($"Ping method failed with status: {result.Status}");
            }
            catch (Exception ex)
            {
                // 保留原始錯誤訊息供上層判斷或記錄
                throw new ApplicationException("Connection failed during Ping.", ex);
            }
        }

        /// <summary>
        /// 取得通用參數及環境設置，進行初始化。
        /// </summary>
        public void Initialize()
        {
            var args = new GetCommonConfigurationArgs();
            var result = Execute<GetCommonConfigurationResult>(SystemActions.GetCommonConfiguration, args, PayloadFormat.Plain);
            var commonConfiguration = result.CommonConfiguration;
            commonConfiguration.Initialize();
        }

        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public ExecFuncResult ExecFunc(ExecFuncArgs args)
        {
            return Execute<ExecFuncResult>(SystemActions.ExecFunc, args);
        }

        /// <summary>
        /// 建立連線。
        /// </summary>
        /// <param name="userID">用戶帳號。</param>
        /// <param name="expiresIn">到期秒數，預設 3600 秒。</param>
        /// <param name="oneTime">一次性有效。</param>
        public Guid CreateSession(string userID, int expiresIn = 3600, bool oneTime = false)
        {
            var args = new CreateSessionArgs()
            {
                UserID = userID,
                ExpiresIn = expiresIn,
                OneTime = oneTime
            };
            var result = Execute<CreateSessionResult>(SystemActions.CreateSession, args, PayloadFormat.Plain);
            return result.AccessToken;
        }

        /// <summary>
        /// 執行登入操作。
        /// </summary>
        /// <param name="userID">使用者帳號。</param>
        /// <param name="password">使用者密碼。</param>
        /// <returns></returns>
        public void Login(string userID, string password)
        {
            // 產生 RSA 對稱金鑰
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml, out var privateKeyXml);

            var args = new LoginArgs()
            {
                UserId = userID,
                Password = password,
                ClientPublicKey = publicKeyXml
            };
            var result = Execute<LoginResult>(SystemActions.Login, args, PayloadFormat.Encoded);
            // 用私鑰解密 EncryptedSessionKey
            string sessionKey = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKeyXml);
            FrontendInfo.ApiEncryptionKey = Convert.FromBase64String(sessionKey);
        }

        /// <summary>
        /// 取得定義資料。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        public T GetDefine<T>(DefineType defineType, string[] keys = null)
        {
            var args = new GetDefineArgs()
            {
                DefineType = defineType,
                Keys = keys
            };
            var result = Execute<GetDefineResult>(SystemActions.GetDefine, args);
            if (StrFunc.IsNotEmpty(result.Xml))
                return SerializeFunc.XmlToObject<T>(result.Xml);
            else
                return default;
        }

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        public void SaveDefine(DefineType defineType, object defineObject, string[] keys = null)
        {
            var args = new SaveDefineArgs()
            {
                DefineType = defineType,
                Xml = SerializeFunc.ObjectToXml(defineObject),
                Keys = keys
            };
            Execute<SaveDefineResult>(SystemActions.SaveDefine, args);
        }
    }
}
