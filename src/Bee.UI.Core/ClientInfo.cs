using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.System;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
using System.Net.Sockets;
using System.Reflection;

namespace Bee.UI.Core
{
    /// <summary>
    /// Provides client-side connection state and access to API connectors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WARNING: this holds <b>one signed-in user's</b> state in process-wide statics — the access
    /// token, the capability snapshot, the entered company, and a definition cache carrying that
    /// user's tenant customization. That is correct for a desktop head, where one process serves one
    /// user, and <b>wrong for a host that serves several users from one process</b>: every session
    /// would write the same fields, so the last sign-in wins and earlier users would read someone
    /// else's identity.
    /// </para>
    /// <para>
    /// This is a stated limitation, not an oversight — but nothing in the type system marks the
    /// boundary, so a server-side UI head built on this package inherits the defect silently.
    /// <c>Bee.Api.Client</c> already went through this: its per-user statics moved to
    /// <see cref="ApiSessionContext"/>, one instance per session. A multi-user head needs the same
    /// treatment here before using this type.
    /// </para>
    /// <para>
    /// Deployment-level values are a different matter and belong exactly where they are:
    /// <see cref="EndpointStorage"/> and <see cref="ApiKeyStorage"/> identify the application, not
    /// a user, so making them per-session would be wrong rather than safer.
    /// </para>
    /// </remarks>
    public static class ClientInfo
    {
        /// <summary>
        /// Guards the lazily created singletons and the resets that clear them.
        /// </summary>
        /// <remarks>
        /// The getters are reached from the UI thread and from continuations of
        /// <c>ConfigureAwait(false)</c> awaits, so two threads can enter the same <c>??=</c> at once.
        /// The wasted instance is not the problem — the orphan is: <see cref="ResetDefineCache"/>
        /// would clear whichever one it happens to hold while callers keep using the other, and a
        /// tenant switch would leave the old customization in play.
        /// <para>
        /// A <see cref="Lazy{T}"/> would not fit: these are deliberately resettable (a new access
        /// token drops the connector and the definition cache), and replacing the <c>Lazy</c>
        /// instance only moves the race.
        /// </para>
        /// </remarks>
        private static readonly Lock s_stateGate = new();

        private static ClientSettings? s_clientSettings;
        private static SystemApiConnector? s_systemConnector;
        private static ClientDefineAccess? s_defineAccess;
        private static Guid s_accessToken = Guid.Empty;
        private static IReadOnlyDictionary<string, PermissionAction>? s_capabilities;
        private static CompanyInfo? s_company;

        /// <summary>
        /// Command-line arguments parsed at <see cref="InitializeAsync(IUIViewService, SupportedConnectTypes)"/>.
        /// </summary>
        public static Dictionary<string, string>? Arguments { get; private set; }

        /// <summary>
        /// Endpoint persistence backend.
        /// </summary>
        public static IEndpointStorage EndpointStorage { get; set; } = new EndpointStorage();

        /// <summary>
        /// Gets or sets the API key persistence strategy. Hosts that replace
        /// <see cref="EndpointStorage"/> because their platform cannot write beside the assembly
        /// (iOS, Android, browser WASM) must replace this too — otherwise the key falls back to a
        /// settings file those platforms cannot persist.
        /// </summary>
        public static IApiKeyStorage ApiKeyStorage { get; set; } = new ApiKeyStorage();

        /// <summary>
        /// Client settings loaded from <c>{ExeName}.Settings.xml</c>.
        /// </summary>
        public static ClientSettings ClientSettings
        {
            get
            {
                lock (s_stateGate) { return s_clientSettings ??= LoadClientSettings(); }
            }
        }

        private static ClientSettings LoadClientSettings()
        {
            string exeName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Client";
            string fileName = $"{exeName}.Settings.xml";
            string filePath = Path.Combine(FileUtilities.GetAssemblyPath(), fileName);

            if (File.Exists(filePath))
            {
                return XmlCodec.DeserializeFromFile<ClientSettings>(filePath)
                    ?? throw new InvalidOperationException($"Failed to deserialize client settings: {filePath}");
            }

            var settings = new ClientSettings();
            settings.SetObjectFilePath(filePath);
            return settings;
        }

        /// <summary>
        /// Access token issued on a successful login.
        /// </summary>
        public static Guid AccessToken
        {
            get { return s_accessToken; }
            private set
            {
                // NOTE: resetting the access token has to clear the `SystemApiConnector` and
                // `ClientDefineAccess` caches with it. Otherwise later calls carry the old token
                // and fail against the server.
                if (value != s_accessToken)
                {
                    // One identity change, one visible step: a reader must not be able to catch a
                    // new token paired with the previous identity's capability snapshot.
                    lock (s_stateGate)
                    {
                        s_accessToken = value;
                        s_systemConnector = null;
                        s_defineAccess = null;
                        // A new (or cleared) token means a different identity — the cached capability
                        // snapshot no longer applies. Reset to null so degradation is disabled until
                        // the next EnterCompany populates it.
                        s_capabilities = null;
                    }
                }
            }
        }

        /// <summary>
        /// System-level API connector. Recreated whenever the endpoint changes.
        /// </summary>
        public static SystemApiConnector SystemApiConnector
        {
            get
            {
                lock (s_stateGate) { return s_systemConnector ??= CreateSystemApiConnector(); }
            }
        }

        private static SystemApiConnector CreateSystemApiConnector()
        {
            return ApiClientInfo.ConnectType == ConnectType.Local
                ? new SystemApiConnector(AccessToken)
                : new SystemApiConnector(ApiClientInfo.Endpoint, AccessToken);
        }

        /// <summary>
        /// Creates a form-level API connector for the specified program.
        /// </summary>
        /// <param name="progId">Program identifier.</param>
        public static FormApiConnector CreateFormApiConnector(string progId)
        {
            return ApiClientInfo.ConnectType == ConnectType.Local
                ? new FormApiConnector(AccessToken, progId)
                : new FormApiConnector(ApiClientInfo.Endpoint, AccessToken, progId);
        }

        /// <summary>
        /// Creates an audit-log API connector (read-only queries over the <c>st_log_*</c> tables).
        /// </summary>
        public static LogApiConnector CreateLogApiConnector()
        {
            return ApiClientInfo.ConnectType == ConnectType.Local
                ? new LogApiConnector(AccessToken)
                : new LogApiConnector(ApiClientInfo.Endpoint, AccessToken);
        }

        /// <summary>
        /// Definition-data accessor. Recreated whenever the endpoint changes.
        /// </summary>
        public static ClientDefineAccess DefineAccess
        {
            get
            {
                lock (s_stateGate) { return s_defineAccess ??= new ClientDefineAccess(SystemApiConnector); }
            }
        }

        /// <summary>
        /// Discards the locally cached definition data.
        /// </summary>
        /// <remarks>
        /// Called automatically by <see cref="ApplyEnterCompanyResult"/> and
        /// <see cref="ClearCompanyContext"/>, which is where a tenant switch actually happens —
        /// hosts do not need to remember it. Exposed for the rare case of discarding the cache
        /// without a tenant change. No-op when the accessor has not been created yet.
        /// <para>
        /// The flush matters because <see cref="ClientDefineAccess"/> keys its cache by
        /// progId / layoutId / namespace alone. The customization layer it holds belongs to whichever
        /// tenant was current when it was fetched, so entries that outlive the switch would serve the
        /// previous tenant's customization to the next one.
        /// </para>
        /// </remarks>
        public static void ResetDefineCache()
        {
            ClientDefineAccess? defineAccess;
            lock (s_stateGate) { defineAccess = s_defineAccess; }
            defineAccess?.ClearCache();
        }

        /// <summary>
        /// UI view service supplied by the host application.
        /// </summary>
        public static IUIViewService? UIViewService { get; private set; }

        /// <summary>
        /// Whether <c>System.Settings.xml</c> and <c>Database.Settings.xml</c> should be auto-generated
        /// when the local endpoint is missing the expected files.
        /// </summary>
        public static bool AllowGenerateSettings { get; set; }

        /// <summary>
        /// Authenticated user information set by <see cref="ApplyLoginResult"/>.
        /// </summary>
        public static UserInfo? UserInfo { get; private set; }

        /// <summary>
        /// The per-model capability snapshot for the entered company, or <c>null</c> when no company
        /// context is active (before <see cref="ApplyEnterCompanyResult"/>, or after
        /// <see cref="ClearCompanyContext"/> / a token change).
        /// </summary>
        /// <remarks>
        /// <c>null</c> means capability enforcement is inactive and the element capability resolver
        /// leaves every element at full capability — so an app that never enters a company (or does
        /// not use permissions) renders unchanged. When non-null, a model absent from the map means
        /// no permission on that model. This is UX degradation only; the backend remains the
        /// authoritative security boundary.
        /// </remarks>
        public static IReadOnlyDictionary<string, PermissionAction>? Capabilities => s_capabilities;

        /// <summary>
        /// Gets the current company entered through <c>EnterCompany</c>, or <c>null</c> when no company
        /// context is active. Carries the company-level decimal-place overrides and default (home)
        /// currency used to round computed numeric fields client-side. Read-only UX aid; the server rounds authoritatively on save.
        /// </summary>
        public static CompanyInfo? Company => s_company;

        /// <summary>
        /// Caches the capability snapshot and company info from an <c>EnterCompany</c> response, and
        /// discards definitions cached for the previous tenant. The host calls this after
        /// <see cref="SystemApiConnector.EnterCompanyAsync"/>.
        /// </summary>
        /// <remarks>
        /// The cache flush is done here rather than left to the caller. Entering a company is exactly
        /// the moment the tenant changes, and a host that forgets to flush gets the previous tenant's
        /// customized layouts and captions with no error to point at it — a cross-tenant leak that
        /// only shows up as wrong text on screen.
        /// </remarks>
        /// <param name="response">The EnterCompany response carrying the capability snapshot and company.</param>
        public static void ApplyEnterCompanyResult(EnterCompanyResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);
            s_capabilities = response.Capabilities;
            s_company = response.Company;
            ResetDefineCache();
        }

        /// <summary>
        /// Clears the cached capability snapshot, company info and definition cache. The host calls
        /// this on <c>LeaveCompany</c> / logout so nothing from the previous tenant survives.
        /// </summary>
        /// <remarks>
        /// Mirrors <see cref="ApplyEnterCompanyResult"/>: leaving a company is a tenant change too,
        /// so the definition cache is flushed here rather than left to the caller.
        /// </remarks>
        public static void ClearCompanyContext()
        {
            s_capabilities = null;
            s_company = null;
            ResetDefineCache();
        }

        private static void SetConnectType(ConnectType connectType, string endpoint)
        {
            if (connectType == ConnectType.Local)
            {
                ApiClientInfo.ConnectType = ConnectType.Local;
                ApiClientInfo.Endpoint = string.Empty;
            }
            else
            {
                ApiClientInfo.ConnectType = ConnectType.Remote;
                ApiClientInfo.Endpoint = endpoint;
            }
            // NOTE: changing the connection method always invalidates the existing token, so a
            // fresh sign-in is required.
            AccessToken = Guid.Empty;
            // The time zone goes with it. Once the session is gone that zone belongs to nobody,
            // and leaving it behind means it would be used for conversions before the next sign-in
            // (ADR-032 D13). `ApplyLoginResult` fills it in again on the way back.
            ApiClientInfo.UserTimeZoneId = string.Empty;
        }

        /// <summary>
        /// Sets the service endpoint and persists it, awaiting the validation and connector
        /// initialization instead of blocking on them.
        /// </summary>
        /// <param name="endpoint">URL for remote connections; local file path for local connections.</param>
        /// <remarks>
        /// Validates the endpoint and initializes the connector without blocking, so it is safe on
        /// single-threaded runtimes (browser WASM), where blocking on async work throws
        /// "Cannot wait on monitors on this runtime".
        /// </remarks>
        public static async Task SetEndpointAsync(string endpoint)
        {
            var connectType = await ApiConnectValidator.ValidateAsync(endpoint, AllowGenerateSettings).ConfigureAwait(false);
            SetConnectType(connectType, endpoint);
            await SystemApiConnector.InitializeAsync().ConfigureAwait(false);
            EndpointStorage.SaveEndpoint(endpoint);
        }

        /// <summary>
        /// Returns the currently configured service endpoint.
        /// </summary>
        public static string GetEndpoint()
        {
            return EndpointStorage.LoadEndpoint();
        }

        /// <summary>
        /// Returns the currently configured API key.
        /// </summary>
        public static string GetApiKey()
        {
            return ApiKeyStorage.LoadApiKey();
        }

        /// <summary>
        /// Persists the API key and applies it to subsequent API calls.
        /// </summary>
        /// <param name="apiKey">The API key issued for this application.</param>
        /// <remarks>
        /// Synchronous, unlike <see cref="SetEndpointAsync"/>: changing the endpoint has to
        /// revalidate the connection and rebuild the connector, whereas the key is simply a header
        /// value the next call carries.
        /// </remarks>
        public static void SetApiKey(string apiKey)
        {
            ApiKeyStorage.SaveApiKey(apiKey);
            ApiClientInfo.ApiKey = apiKey;
        }

        /// <summary>
        /// Applies the stored API key to <see cref="ApiClientInfo.ApiKey"/>, falling back to
        /// <paramref name="defaultApiKey"/> — and persisting it — the first time an application runs
        /// with nothing stored.
        /// </summary>
        /// <param name="defaultApiKey">
        /// The key the application ships with, used only to seed empty storage. Pass an empty string
        /// for deployments that expect the key to be configured out of band.
        /// </param>
        /// <remarks>
        /// This is what lets an application drop its hard-coded key without losing out-of-the-box
        /// behaviour: the shipped value becomes a first-run seed, and from then on the stored value
        /// wins and can be changed without recompiling.
        /// </remarks>
        public static void ApplyApiKey(string defaultApiKey = "")
        {
            string stored = ApiKeyStorage.LoadApiKey();
            if (StringUtilities.IsEmpty(stored) && StringUtilities.IsNotEmpty(defaultApiKey))
            {
                ApiKeyStorage.SaveApiKey(defaultApiKey);
                stored = defaultApiKey;
            }
            ApiClientInfo.ApiKey = stored;
        }

        private static async Task<bool> InitializeConnectAsync(SupportedConnectTypes supportedConnectTypes)
        {
            ApiClientInfo.SupportedConnectTypes = supportedConnectTypes;
            try
            {
                string endpoint = GetEndpoint();
                var connectType = await ApiConnectValidator.ValidateAsync(endpoint, AllowGenerateSettings).ConfigureAwait(false);
                SetConnectType(connectType, endpoint);
                await SystemApiConnector.InitializeAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException
                or OperationCanceledException or IOException or SocketException or UriFormatException)
            {
                // Returning false sends the caller to the connection-setup view, so only "the
                // endpoint is missing, malformed or unreachable" belongs here. The first two entries
                // are the vocabulary this path actually raises — `ApiConnectValidator` reports an
                // unreachable endpoint as `InvalidOperationException` and an empty one as
                // `ArgumentException`; the rest cover the transport failures underneath it.
                return false;
            }
        }

        /// <summary>
        /// Initializes from settings. Falls back to the connection setup view when the endpoint is
        /// missing or unreachable.
        /// </summary>
        /// <param name="service">UI view service supplied by the host application.</param>
        /// <param name="connectTypes">Connection types supported by the application.</param>
        public static async Task<bool> InitializeAsync(IUIViewService service, SupportedConnectTypes connectTypes)
        {
            UIViewService = service;
            Arguments = ParseCommandLineArgs();
            if (Arguments.TryGetValue("Endpoint", out string? endpointArg))
            {
                EndpointStorage.SetEndpoint(endpointArg);
            }
            // NOTE: in-memory only, like the endpoint argument — a command line switch overrides
            // this run without rewriting stored settings. Suitable because the key identifies an
            // application rather than authenticating a user; a real credential does not belong in
            // an argument list, which is readable from the process table.
            if (Arguments.TryGetValue("ApiKey", out string? apiKeyArg))
            {
                ApiKeyStorage.SetApiKey(apiKeyArg);
            }
            if (!await InitializeConnectAsync(connectTypes).ConfigureAwait(false)
                && !await UIViewService.ShowApiConnectAsync().ConfigureAwait(false))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initializes with an explicit endpoint, awaiting the validation and connector
        /// initialization without blocking.
        /// </summary>
        /// <param name="endpoint">URL for remote connections; local file path for local connections.</param>
        /// <remarks>
        /// Safe on single-threaded runtimes (browser WASM), where blocking on async work throws
        /// "Cannot wait on monitors".
        /// </remarks>
        public static Task InitializeAsync(string endpoint)
        {
            return SetEndpointAsync(endpoint);
        }

        /// <summary>
        /// Applies the login response, populating <see cref="AccessToken"/> and <see cref="UserInfo"/>.
        /// </summary>
        /// <param name="loginResponse">Result returned from the login API.</param>
        public static void ApplyLoginResult(LoginResponse loginResponse)
        {
            ArgumentNullException.ThrowIfNull(loginResponse);

            AccessToken = loginResponse.AccessToken;
            UserInfo = new UserInfo()
            {
                UserId = loginResponse.UserId,
                UserName = loginResponse.UserName,
                // The server's value is authoritative; an empty one leaves the UserInfo default
                // rather than silently adopting the device zone, which ADR-032 D4 rules out.
                TimeZone = StringUtilities.IsNotEmpty(loginResponse.TimeZone)
                    ? loginResponse.TimeZone
                    : new UserInfo().TimeZone
            };
            // The Connector layer sits below this one, so it cannot read UserInfo — hand it the zone
            // it needs to convert payloads with (ADR-032 D4).
            ApiClientInfo.UserTimeZoneId = UserInfo.TimeZone;
            // NOTE: any further post-sign-in state belongs here.
        }

        private static Dictionary<string, string> ParseCommandLineArgs()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                int sep = args[i].IndexOf('=');
                if (sep > 0)
                {
                    string key = args[i].Substring(0, sep);
                    string value = args[i].Substring(sep + 1);
                    result[key] = value;
                }
            }
            return result;
        }

    }
}
