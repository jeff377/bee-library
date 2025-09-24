using System;
using System.Threading.Tasks;
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
        /// 非同步執行 API 方法。
        /// </summary>
        /// <param name="action">執行動作。</param>
        /// <param name="value">對應執行動作的傳入參數。</param>
        /// <param name="format">傳輸資料的封裝格式。</param>
        public async Task<T> ExecuteAsync<T>(string action, object value, PayloadFormat format = PayloadFormat.Encrypted)
        {
            return await base.ExecuteAsync<T>(SysProgIds.System, action, value, format).ConfigureAwait(false);
        }

        /// <summary>
        /// 非同步執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public async Task<ExecFuncResult> ExecFuncAsync(ExecFuncArgs args)
        {
            return await ExecuteAsync<ExecFuncResult>(SystemActions.ExecFunc, args).ConfigureAwait(false);
        }

        /// <summary>
        /// 執行自訂方法。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public ExecFuncResult ExecFunc(ExecFuncArgs args)
        {
            return SyncExecutor.Run(() =>
                ExecFuncAsync(args)
            );
        }

        /// <summary>
        /// 非同步執行自訂方法，匿名存取。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public async Task<ExecFuncResult> ExecFuncAnonymousAsync(ExecFuncArgs args)
        {
            return await ExecuteAsync<ExecFuncResult>(SystemActions.ExecFuncAnonymous, args, PayloadFormat.Encoded).ConfigureAwait(false);
        }

        /// <summary>
        /// 非同步執行自訂方法，僅限近端呼叫。
        /// </summary>
        /// <param name="args">傳入引數。</param>
        public async Task<ExecFuncResult> ExecFuncLocalAsync(ExecFuncArgs args)
        {
            return await ExecuteAsync<ExecFuncResult>(SystemActions.ExecFuncLocal, args).ConfigureAwait(false);
        }

        /// <summary>
        /// 執行 Ping 方法，測試伺服端的連線狀態。
        /// </summary>
        public async Task PingAsync()
        {
            try
            {
                var args = new PingArgs()
                {
                    ClientName = "Connector",
                    TraceId = Guid.NewGuid().ToString()
                };
                var result = await ExecuteAsync<PingResult>(SystemActions.Ping, args, PayloadFormat.Plain).ConfigureAwait(false);
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
        ///  非同步執行 Ping 方法，測試伺服端的連線狀態。
        /// </summary>
        public void Ping()
        {
            SyncExecutor.Run(() =>
                PingAsync()
            );
        }

        /// <summary>
        /// 非同步取得通用參數及環境設置，進行初始化。
        /// </summary>
        public async Task InitializeAsync()
        {
            // 取得通用參數及環境設置，進行初始化
            var args = new GetCommonConfigurationArgs();
            var result = await ExecuteAsync<GetCommonConfigurationResult>(SystemActions.GetCommonConfiguration, args, PayloadFormat.Plain).ConfigureAwait(false);
            var configuration = SerializeFunc.XmlToObject<CommonConfiguration>(result.CommonConfiguration);
            configuration.Initialize();
            // 初始化 API 服務選項，設定序列化器、壓縮器與加密器的實作
            ApiServiceOptions.Initialize(configuration.ApiPayloadOptions);
        }

        /// <summary>
        /// 取得通用參數及環境設置，進行初始化。
        /// </summary>
        public void Initialize()
        {
            SyncExecutor.Run(() =>
                InitializeAsync()
            );
        }

        /// <summary>
        /// 非同步建立連線。
        /// </summary>
        /// <param name="userID">用戶帳號。</param>
        /// <param name="expiresIn">到期秒數，預設 3600 秒。</param>
        /// <param name="oneTime">一次性有效。</param>
        public async Task<Guid> CreateSessionAsync(string userID, int expiresIn = 3600, bool oneTime = false)
        {
            var args = new CreateSessionArgs()
            {
                UserID = userID,
                ExpiresIn = expiresIn,
                OneTime = oneTime
            };
            var result = await ExecuteAsync<CreateSessionResult>(SystemActions.CreateSession, args, PayloadFormat.Plain).ConfigureAwait(false);
            return result.AccessToken;
        }

        /// <summary>
        /// 建立連線。
        /// </summary>
        /// <param name="userID">用戶帳號。</param>
        /// <param name="expiresIn">到期秒數，預設 3600 秒。</param>
        /// <param name="oneTime">一次性有效。</param>
        public Guid CreateSession(string userID, int expiresIn = 3600, bool oneTime = false)
        {
            return SyncExecutor.Run(() =>
                CreateSessionAsync(userID, expiresIn, oneTime)
            );
        }

        /// <summary>
        /// 非同步執行登入操作。
        /// </summary>
        /// <param name="userID">使用者帳號。</param>
        /// <param name="password">使用者密碼。</param>
        public async Task<LoginResult> LoginAsync(string userID, string password)
        {
            // 產生 RSA 對稱金鑰
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml, out var privateKeyXml);

            // 執行登入操作
            var args = new LoginArgs()
            {
                UserId = userID,
                Password = password,
                ClientPublicKey = publicKeyXml  // 傳入 RSA 公鑰
            };
            var result = await ExecuteAsync<LoginResult>(SystemActions.Login, args, PayloadFormat.Encoded).ConfigureAwait(false);

            // 取得存取令牌
            FrontendInfo.AccessToken = result.AccessToken;

            // 用 RSA 私鑰解密，取得 API 加密金鑰
            string sessionKey = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKeyXml);
            FrontendInfo.ApiEncryptionKey = Convert.FromBase64String(sessionKey);

            return result;
        }

        /// <summary>
        /// 執行登入操作。
        /// </summary>
        /// <param name="userID">使用者帳號。</param>
        /// <param name="password">使用者密碼。</param>
        public LoginResult Login(string userID, string password)
        {
            return SyncExecutor.Run(() =>
                LoginAsync(userID, password)
            );
        }

        /// <summary>
        /// 非同步取得定義資料。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        public async Task<T> GetDefineAsync<T>(DefineType defineType, string[] keys = null)
        {
            var args = new GetDefineArgs()
            {
                DefineType = defineType,
                Keys = keys
            };
            var result = await ExecuteAsync<GetDefineResult>(SystemActions.GetDefine, args).ConfigureAwait(false);
            if (StrFunc.IsNotEmpty(result.Xml))
                return SerializeFunc.XmlToObject<T>(result.Xml);
            else
                return default;
        }

        /// <summary>
        /// 取得定義資料。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        public T GetDefine<T>(DefineType defineType, string[] keys = null)
        {
            return SyncExecutor.Run(() =>
                GetDefineAsync<T>(defineType, keys)
            );
        }

        /// <summary>
        /// 非同步取得定義資料（僅限本機）。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        public async Task<T> GetLocalDefineAsync<T>(DefineType defineType, string[] keys = null)
        {
            var args = new GetDefineArgs()
            {
                DefineType = defineType,
                Keys = keys
            };
            var result = await ExecuteAsync<GetDefineResult>(SystemActions.GetLocalDefine, args).ConfigureAwait(false);
            if (StrFunc.IsNotEmpty(result.Xml))
                return SerializeFunc.XmlToObject<T>(result.Xml);
            else
                return default;
        }

        /// <summary>
        /// 取得定義資料（僅限本機）。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="keys">取得定義資料的鍵值。</param>
        public T GetLocalDefine<T>(DefineType defineType, string[] keys = null)
        {
            return SyncExecutor.Run(() =>
                GetLocalDefineAsync<T>(defineType, keys)
            );
        }

        /// <summary>
        /// 非同步儲存定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        public async Task SaveDefineAsync(DefineType defineType, object defineObject, string[] keys = null)
        {
            var args = new SaveDefineArgs()
            {
                DefineType = defineType,
                Xml = SerializeFunc.ObjectToXml(defineObject),
                Keys = keys
            };
            await ExecuteAsync<SaveDefineResult>(SystemActions.SaveDefine, args).ConfigureAwait(false);
        }

        /// <summary>
        /// 儲存定義資料。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        public void SaveDefine(DefineType defineType, object defineObject, string[] keys = null)
        {
            SyncExecutor.Run(() =>
                SaveDefineAsync(defineType, defineObject, keys)
            );
        }

        /// <summary>
        /// 非同步儲存定義資料（僅限本機）。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        public async Task SaveLocalDefineAsync(DefineType defineType, object defineObject, string[] keys = null)
        {
            var args = new SaveDefineArgs()
            {
                DefineType = defineType,
                Xml = SerializeFunc.ObjectToXml(defineObject),
                Keys = keys
            };
            await ExecuteAsync<SaveDefineResult>(SystemActions.SaveLocalDefine, args).ConfigureAwait(false);
        }

        /// <summary>
        /// 儲存定義資料（僅限本機）。
        /// </summary>
        /// <param name="defineType">定義資料類型。</param>
        /// <param name="defineObject">定義資料。</param>
        /// <param name="keys">儲存定義資料的鍵值。</param>
        public void SaveLocalDefine(DefineType defineType, object defineObject, string[] keys = null)
        {
            SyncExecutor.Run(() =>
                SaveLocalDefineAsync(defineType, defineObject, keys)
            );
        }
    }
}
