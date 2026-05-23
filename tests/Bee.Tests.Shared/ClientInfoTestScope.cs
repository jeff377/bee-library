using System.Reflection;
using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Definition.Identity;
using Bee.Definition.Storage;
using Bee.UI.Core;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Captures the mutable static state of <see cref="ClientInfo"/> and
    /// <see cref="ApiClientInfo"/> at construction, lets tests freely mutate both
    /// during the test body, and restores the original snapshot on
    /// <see cref="Dispose"/>. Any test class that touches either type must use
    /// this scope <em>and</em> opt in to xUnit's <c>[Collection("ClientInfo")]</c>
    /// so concurrent test classes don't race on the same process-wide statics.
    /// </summary>
    /// <remarks>
    /// The snapshot covers everything <see cref="ClientInfo"/> and
    /// <see cref="ApiClientInfo"/> expose with a non-trivial setter — connection
    /// type, endpoint, access token, cached connectors, login-derived user info,
    /// and the local backend service provider. Cached connectors are restored
    /// verbatim (not rebuilt) because rebuilding can fail when the test left the
    /// process in a state the production validators reject.
    /// </remarks>
    public sealed class ClientInfoTestScope : IDisposable
    {
        private static readonly FieldInfo AccessTokenField =
            typeof(ClientInfo).GetField("_accessToken", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ClientInfo._accessToken not found.");

        private static readonly FieldInfo SystemConnectorField =
            typeof(ClientInfo).GetField("_systemConnector", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ClientInfo._systemConnector not found.");

        private static readonly FieldInfo DefineAccessField =
            typeof(ClientInfo).GetField("_defineAccess", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ClientInfo._defineAccess not found.");

        private static readonly PropertyInfo UserInfoProperty =
            typeof(ClientInfo).GetProperty(nameof(ClientInfo.UserInfo),
                BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ClientInfo.UserInfo property not found.");

        private readonly SupportedConnectTypes _supportedConnectTypes;
        private readonly ConnectType _connectType;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly byte[] _apiEncryptionKey;
        private readonly IServiceProvider? _localServiceProvider;

        private readonly bool _allowGenerateSettings;
        private readonly IEndpointStorage _endpointStorage;
        private readonly IUIViewService? _uiViewService;
        private readonly Guid _accessToken;
        private readonly SystemApiConnector? _systemConnector;
        private readonly IDefineAccess? _defineAccess;
        private readonly UserInfo? _userInfo;

        private bool _disposed;

        /// <summary>
        /// Snapshots the current <see cref="ClientInfo"/> / <see cref="ApiClientInfo"/>
        /// state. Subsequent mutations are restored on <see cref="Dispose"/>.
        /// </summary>
        public ClientInfoTestScope()
        {
            _supportedConnectTypes = ApiClientInfo.SupportedConnectTypes;
            _connectType = ApiClientInfo.ConnectType;
            _endpoint = ApiClientInfo.Endpoint;
            _apiKey = ApiClientInfo.ApiKey;
            _apiEncryptionKey = ApiClientInfo.ApiEncryptionKey;
            _localServiceProvider = ApiClientInfo.LocalServiceProvider;

            _allowGenerateSettings = ClientInfo.AllowGenerateSettings;
            _endpointStorage = ClientInfo.EndpointStorage;
            _uiViewService = ClientInfo.UIViewService;
            _accessToken = (Guid)AccessTokenField.GetValue(null)!;
            _systemConnector = (SystemApiConnector?)SystemConnectorField.GetValue(null);
            _defineAccess = (IDefineAccess?)DefineAccessField.GetValue(null);
            _userInfo = ClientInfo.UserInfo;
        }

        /// <summary>
        /// Overwrites <see cref="ClientInfo.AccessToken"/> and invalidates the
        /// cached <see cref="SystemApiConnector"/> / <see cref="IDefineAccess"/>
        /// so subsequent accesses rebuild them against the new token.
        /// </summary>
        /// <param name="token">The token value to install for the duration of the scope.</param>
        public void SetAccessToken(Guid token)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            AccessTokenField.SetValue(null, token);
            SystemConnectorField.SetValue(null, null);
            DefineAccessField.SetValue(null, null);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ApiClientInfo.SupportedConnectTypes = _supportedConnectTypes;
            ApiClientInfo.ConnectType = _connectType;
            ApiClientInfo.Endpoint = _endpoint;
            ApiClientInfo.ApiKey = _apiKey;
            ApiClientInfo.ApiEncryptionKey = _apiEncryptionKey;
            ApiClientInfo.LocalServiceProvider = _localServiceProvider;

            ClientInfo.AllowGenerateSettings = _allowGenerateSettings;
            ClientInfo.EndpointStorage = _endpointStorage;
            AccessTokenField.SetValue(null, _accessToken);
            SystemConnectorField.SetValue(null, _systemConnector);
            DefineAccessField.SetValue(null, _defineAccess);
            UserInfoProperty.SetValue(null, _userInfo);

            // ClientInfo.UIViewService has a private setter; restore via reflection.
            var viewServiceProperty = typeof(ClientInfo).GetProperty(
                nameof(ClientInfo.UIViewService), BindingFlags.Public | BindingFlags.Static);
            viewServiceProperty?.SetValue(null, _uiViewService);
        }
    }
}
