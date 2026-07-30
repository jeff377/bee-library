using Bee.Definition.Settings;
using Bee.Api.Core;
using Bee.Base;
using Bee.Base.Security;
using Bee.Base.Serialization;
using Bee.Api.Core.Messages.System;
using Bee.Definition;
using Bee.Definition.Organization;
using Bee.Definition.Security;
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

        // Note: GetFormSchema, GetFormLayout and GetLanguage have no .NET client methods. Those BO
        // methods are JS-only (Plain JSON wire format) — see the SystemBO XML docs for the
        // rationale. .NET clients use GetDefineAsync with the matching DefineType, which carries
        // the definition as XML.
        //
        // The distinction is not stylistic. Definition types (FormSchema, FormLayout,
        // LanguageResource, TableSchema) declare XML as their serialisation contract: their nested
        // collections are get-only, which XmlSerializer handles by populating the existing instance.
        // JSON and MessagePack instead bind by writability, so they serialise those collections on
        // the way out but drop them on the way back — a .NET caller would receive a schema carrying
        // its scalar fields and no tables, with no error raised. Rather than reshape the definition
        // model to suit a wire format, definitions travel as XML and these entry points stay out of
        // the .NET client.

        /// <summary>
        /// Asynchronously gets the current company's department tree (per-company organisation
        /// hierarchy). JSON-friendly for frontends; returns <c>null</c> when no company is entered.
        /// </summary>
        public virtual async Task<DepartmentTree?> GetDepartmentTreeAsync()
        {
            var request = new GetDepartmentTreeRequest();
            var result = await ExecuteAsync<GetDepartmentTreeResponse>(SystemActions.GetDepartmentTree, request)
                .ConfigureAwait(false);
            return result.Tree;
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
        /// Asynchronously issues a new API key and returns the complete plaintext key once.
        /// </summary>
        /// <param name="sysId">The key identifier to issue; lowercase letters, digits and hyphens.</param>
        /// <param name="sysName">The display name of the application the key is for.</param>
        /// <param name="keyType">The key classification; a label only.</param>
        /// <param name="contact">The contact for a third-party holder, so an incident has someone to reach.</param>
        /// <param name="expiredAt">The UTC expiry, or <c>null</c> for a key that does not expire.</param>
        /// <remarks>
        /// IMPORTANT: <c>CreateApiKeyResponse.ApiKey</c> is the only time the plaintext key exists
        /// outside the caller — the server keeps just a hash. Persist it here or issue a replacement.
        /// <para>
        /// The server restricts this to local calls, so this reaches it through an in-process
        /// connector only.
        /// </para>
        /// </remarks>
        public async Task<CreateApiKeyResponse> CreateApiKeyAsync(string sysId, string sysName,
            ApiKeyType keyType = ApiKeyType.Internal, string? contact = null, DateTime? expiredAt = null)
        {
            var request = new CreateApiKeyRequest()
            {
                SysId = sysId,
                SysName = sysName,
                KeyType = keyType,
                Contact = contact,
                ExpiredAt = expiredAt
            };
            return await ExecuteAsync<CreateApiKeyResponse>(SystemActions.CreateApiKey, request).ConfigureAwait(false);
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
