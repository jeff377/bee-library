using Bee.Definition.Settings;
using System;
using System.Threading.Tasks;
using Bee.Api.Core;
using Bee.Base;
using Bee.Base.Security;
using Bee.Base.Serialization;
using Bee.Api.Core.System;
using Bee.Definition;

namespace Bee.Api.Client.Connectors
{
    /// <summary>
    /// System-level API service connector.
    /// </summary>
    public class SystemApiConnector : ApiConnector
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemApiConnector"/> class using a local connection.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public SystemApiConnector(Guid accessToken) : base(accessToken)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemApiConnector"/> class using a remote connection.
        /// </summary>
        /// <param name="endpoint">The service endpoint.</param>
        /// <param name="accessToken">The access token.</param>
        public SystemApiConnector(string endpoint, Guid accessToken) : base(endpoint, accessToken)
        { }

        #endregion

        /// <summary>
        /// Asynchronously executes an API method.
        /// </summary>
        /// <param name="action">The action name to execute.</param>
        /// <param name="value">The input parameter for the action.</param>
        /// <param name="format">The payload encoding format for transmission.</param>
        public async Task<T> ExecuteAsync<T>(string action, object value, PayloadFormat format = PayloadFormat.Encrypted)
        {
            return await base.ExecuteAsync<T>(SysProgIds.System, action, value, format).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a custom method; requires authentication.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public async Task<ExecFuncResponse> ExecFuncAsync(ExecFuncRequest args)
        {
            return await ExecuteAsync<ExecFuncResponse>(SystemActions.ExecFunc, args).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a custom method; requires authentication.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public ExecFuncResponse ExecFunc(ExecFuncRequest args)
        {
            return SyncExecutor.Run(() =>
                ExecFuncAsync(args)
            );
        }

        /// <summary>
        /// Asynchronously executes a custom method; allows anonymous access.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public async Task<ExecFuncResponse> ExecFuncAnonymousAsync(ExecFuncRequest args)
        {
            return await ExecuteAsync<ExecFuncResponse>(SystemActions.ExecFuncAnonymous, args, PayloadFormat.Encoded).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes a custom method; local calls only.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        public async Task<ExecFuncResponse> ExecFuncLocalAsync(ExecFuncRequest args)
        {
            return await ExecuteAsync<ExecFuncResponse>(SystemActions.ExecFuncLocal, args).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously executes the Ping method to test the server connection status.
        /// </summary>
        public async Task PingAsync()
        {
            try
            {
                var request = new PingRequest()
                {
                    ClientName = "Connector",
                    TraceId = Guid.NewGuid().ToString()
                };
                var result = await ExecuteAsync<PingResponse>(SystemActions.Ping, request, PayloadFormat.Plain).ConfigureAwait(false);
                if (result.Status != "ok")
                    throw new InvalidOperationException($"Ping method failed with status: {result.Status}");
            }
            catch (Exception ex)
            {
                // Preserve the original error message for callers to inspect or log
                throw new InvalidOperationException("Connection failed during Ping.", ex);
            }
        }

        /// <summary>
        /// Executes the Ping method to test the server connection status.
        /// </summary>
        public void Ping()
        {
            SyncExecutor.Run(() =>
                PingAsync()
            );
        }

        /// <summary>
        /// Asynchronously retrieves common parameters and environment configuration, then initializes the system.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Retrieve common parameters and environment configuration for initialization
            var request = new GetCommonConfigurationRequest();
            var result = await ExecuteAsync<GetCommonConfigurationResponse>(SystemActions.GetCommonConfiguration, request, PayloadFormat.Plain).ConfigureAwait(false);
            var configuration = SerializeFunc.XmlToObject<CommonConfiguration>(result.CommonConfiguration)!;
            SysInfo.Initialize(configuration);
            // Initialize API service options: configure serializer, compressor, and encryptor implementations
            ApiServiceOptions.Initialize(configuration.ApiPayloadOptions);
        }

        /// <summary>
        /// Retrieves common parameters and environment configuration, then initializes the system.
        /// </summary>
        public void Initialize()
        {
            SyncExecutor.Run(() =>
                InitializeAsync()
            );
        }

        /// <summary>
        /// Asynchronously creates a new user session.
        /// </summary>
        /// <param name="userID">The user account identifier.</param>
        /// <param name="expiresIn">The expiration time in seconds. Defaults to 3600.</param>
        /// <param name="oneTime">Whether the session is valid for one-time use only.</param>
        public async Task<Guid> CreateSessionAsync(string userID, int expiresIn = 3600, bool oneTime = false)
        {
            var request = new CreateSessionRequest()
            {
                UserID = userID,
                ExpiresIn = expiresIn,
                OneTime = oneTime
            };
            var result = await ExecuteAsync<CreateSessionResponse>(SystemActions.CreateSession, request, PayloadFormat.Plain).ConfigureAwait(false);
            return result.AccessToken;
        }

        /// <summary>
        /// Creates a new user session.
        /// </summary>
        /// <param name="userID">The user account identifier.</param>
        /// <param name="expiresIn">The expiration time in seconds. Defaults to 3600.</param>
        /// <param name="oneTime">Whether the session is valid for one-time use only.</param>
        public Guid CreateSession(string userID, int expiresIn = 3600, bool oneTime = false)
        {
            return SyncExecutor.Run(() =>
                CreateSessionAsync(userID, expiresIn, oneTime)
            );
        }

        /// <summary>
        /// Asynchronously performs the login operation.
        /// </summary>
        /// <param name="userID">The user account identifier.</param>
        /// <param name="password">The user password.</param>
        public async Task<LoginResponse> LoginAsync(string userID, string password)
        {
            // Generate an RSA key pair
            RsaCryptor.GenerateRsaKeyPair(out var publicKeyXml, out var privateKeyXml);

            // Perform the login operation
            var request = new LoginRequest()
            {
                UserId = userID,
                Password = password,
                ClientPublicKey = publicKeyXml  // Pass the RSA public key
            };
            var result = await ExecuteAsync<LoginResponse>(SystemActions.Login, request, PayloadFormat.Encoded).ConfigureAwait(false);

            // Decrypt with the RSA private key to obtain the API encryption key
            string sessionKey = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKeyXml);
            ApiClientContext.ApiEncryptionKey = Convert.FromBase64String(sessionKey);

            return result;
        }

        /// <summary>
        /// Performs the login operation.
        /// </summary>
        /// <param name="userID">The user account identifier.</param>
        /// <param name="password">The user password.</param>
        public LoginResponse Login(string userID, string password)
        {
            return SyncExecutor.Run(() =>
                LoginAsync(userID, password)
            );
        }

        /// <summary>
        /// Asynchronously gets definition data.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="keys">The keys used to locate the definition data.</param>
        public async Task<T> GetDefineAsync<T>(DefineType defineType, string[]? keys = null)
        {
            var request = new GetDefineRequest()
            {
                DefineType = defineType,
                Keys = keys
            };
            var result = await ExecuteAsync<GetDefineResponse>(SystemActions.GetDefine, request).ConfigureAwait(false);
            if (StrFunc.IsNotEmpty(result.Xml))
                return SerializeFunc.XmlToObject<T>(result.Xml)!;
            else
                return default!;
        }

        /// <summary>
        /// Gets definition data.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="keys">The keys used to locate the definition data.</param>
        public T GetDefine<T>(DefineType defineType, string[]? keys = null)
        {
            return SyncExecutor.Run(() =>
                GetDefineAsync<T>(defineType, keys)
            );
        }

        /// <summary>
        /// Asynchronously saves definition data.
        /// </summary>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="defineObject">The definition data object.</param>
        /// <param name="keys">The keys used to locate where the definition data is saved.</param>
        public async Task SaveDefineAsync(DefineType defineType, object defineObject, string[]? keys = null)
        {
            var request = new SaveDefineRequest()
            {
                DefineType = defineType,
                Xml = SerializeFunc.ObjectToXml(defineObject),
                Keys = keys
            };
            await ExecuteAsync<SaveDefineResponse>(SystemActions.SaveDefine, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves definition data.
        /// </summary>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="defineObject">The definition data object.</param>
        /// <param name="keys">The keys used to locate where the definition data is saved.</param>
        public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null)
        {
            SyncExecutor.Run(() =>
                SaveDefineAsync(defineType, defineObject, keys)
            );
        }

    }
}
