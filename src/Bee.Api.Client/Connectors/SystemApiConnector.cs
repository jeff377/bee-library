using Bee.Definition.Settings;
using Bee.Api.Core;
using Bee.Base;
using Bee.Base.Security;
using Bee.Base.Serialization;
using Bee.Api.Core.Messages.System;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Api.Core.Messages;

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
        /// Asynchronously retrieves common parameters and environment configuration, then initializes the system.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Retrieve common parameters and environment configuration for initialization
            var request = new GetCommonConfigurationRequest();
            var result = await ExecuteAsync<GetCommonConfigurationResponse>(SystemActions.GetCommonConfiguration, request, PayloadFormat.Plain).ConfigureAwait(false);
            var configuration = XmlCodec.Deserialize<CommonConfiguration>(result.CommonConfiguration)!;
            SysInfo.Initialize(configuration);
            // Initialize API service options: configure serializer, compressor, and encryptor implementations
            ApiServiceOptions.Initialize(configuration.ApiPayloadOptions, configuration.IsDebugMode);
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
        /// Asynchronously performs the login operation.
        /// </summary>
        /// <remarks>
        /// On Blazor WebAssembly (<see cref="OperatingSystem.IsBrowser"/> returns true), the RSA
        /// handshake is skipped because .NET's RSA key generation is not implemented on the
        /// browser-wasm runtime. <see cref="LoginRequest.ClientPublicKey"/> is sent empty, the
        /// server returns an empty <see cref="LoginResponse.ApiEncryptionKey"/>, and subsequent
        /// <see cref="PayloadFormat.Encrypted"/> requests are auto-downgraded to
        /// <see cref="PayloadFormat.Encoded"/> by <see cref="ApiConnector"/>.
        /// </remarks>
        /// <param name="userID">The user account identifier.</param>
        /// <param name="password">The user password.</param>
        public async Task<LoginResponse> LoginAsync(string userID, string password)
        {
            string publicKey = string.Empty;
            string privateKey = string.Empty;
            bool useRsaHandshake = !OperatingSystem.IsBrowser();
            if (useRsaHandshake)
            {
                RsaCryptor.GenerateRsaKeyPair(out publicKey, out privateKey);
            }

            var request = new LoginRequest()
            {
                UserId = userID,
                Password = password,
                ClientPublicKey = publicKey
            };
            var result = await ExecuteAsync<LoginResponse>(SystemActions.Login, request, PayloadFormat.Encoded).ConfigureAwait(false);

            if (useRsaHandshake && !string.IsNullOrEmpty(result.ApiEncryptionKey))
            {
                string sessionKey = RsaCryptor.DecryptWithPrivateKey(result.ApiEncryptionKey, privateKey);
                ApiClientInfo.ApiEncryptionKey = Convert.FromBase64String(sessionKey);
            }

            return result;
        }

        /// <summary>
        /// Asynchronously gets definition data.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="keys">The keys used to locate the definition data.</param>
        public virtual async Task<T> GetDefineAsync<T>(DefineType defineType, string[]? keys = null)
        {
            var request = new GetDefineRequest()
            {
                DefineType = defineType,
                Keys = keys
            };
            var result = await ExecuteAsync<GetDefineResponse>(SystemActions.GetDefine, request).ConfigureAwait(false);
            if (StringUtilities.IsNotEmpty(result.Xml))
                return XmlCodec.Deserialize<T>(result.Xml)!;
            else
                return default!;
        }

        /// <summary>
        /// Asynchronously gets a form schema as a typed object. JSON-friendly
        /// alternative to <see cref="GetDefineAsync{T}"/> for the FormSchema type,
        /// primarily intended for JS / TypeScript frontends but usable from the
        /// .NET client as well.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public virtual async Task<FormSchema?> GetFormSchemaAsync(string progId)
        {
            var request = new GetFormSchemaRequest() { ProgId = progId };
            var result = await ExecuteAsync<GetFormSchemaResponse>(SystemActions.GetFormSchema, request)
                .ConfigureAwait(false);
            return result.Schema;
        }

        /// <summary>
        /// Synchronously gets a form schema as a typed object.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public virtual FormSchema? GetFormSchema(string progId)
        {
            return SyncExecutor.Run(() => GetFormSchemaAsync(progId));
        }

        /// <summary>
        /// Asynchronously gets a form layout as a typed object. JSON-friendly
        /// alternative for rendering schema-driven UI from JS / TypeScript
        /// frontends; usable from the .NET client as well.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="layoutId">The layout identifier; empty string resolves to <c>"default"</c>.</param>
        public virtual async Task<FormLayout?> GetFormLayoutAsync(string progId, string layoutId = "")
        {
            var request = new GetFormLayoutRequest()
            {
                ProgId = progId,
                LayoutId = layoutId ?? string.Empty,
            };
            var result = await ExecuteAsync<GetFormLayoutResponse>(SystemActions.GetFormLayout, request)
                .ConfigureAwait(false);
            return result.Layout;
        }

        /// <summary>
        /// Synchronously gets a form layout as a typed object.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="layoutId">The layout identifier; empty string resolves to <c>"default"</c>.</param>
        public virtual FormLayout? GetFormLayout(string progId, string layoutId = "")
        {
            return SyncExecutor.Run(() => GetFormLayoutAsync(progId, layoutId));
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
                Xml = XmlCodec.Serialize(defineObject),
                Keys = keys
            };
            await ExecuteAsync<SaveDefineResponse>(SystemActions.SaveDefine, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously enters the specified company for the current session.
        /// Also used to switch between companies — the previous company binding is overwritten.
        /// </summary>
        /// <param name="companyId">The id of the company to enter.</param>
        public async Task<EnterCompanyResponse> EnterCompanyAsync(string companyId)
        {
            var request = new EnterCompanyRequest()
            {
                CompanyId = companyId
            };
            return await ExecuteAsync<EnterCompanyResponse>(SystemActions.EnterCompany, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously clears the company context from the current session.
        /// Idempotent — returns success even if the session has not entered a company.
        /// </summary>
        public async Task<LeaveCompanyResponse> LeaveCompanyAsync()
        {
            var request = new LeaveCompanyRequest();
            return await ExecuteAsync<LeaveCompanyResponse>(SystemActions.LeaveCompany, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously destroys the current session, clearing any company context first.
        /// Idempotent — succeeds even if the session is already expired or unknown.
        /// </summary>
        public async Task<LogoutResponse> LogoutAsync()
        {
            var request = new LogoutRequest();
            return await ExecuteAsync<LogoutResponse>(SystemActions.Logout, request).ConfigureAwait(false);
        }

    }
}
