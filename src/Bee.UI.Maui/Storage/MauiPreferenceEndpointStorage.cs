using Bee.UI.Core;

namespace Bee.UI.Maui.Storage
{
    /// <summary>
    /// <see cref="IEndpointStorage"/> implementation backed by MAUI
    /// <see cref="Microsoft.Maui.Storage.Preferences"/>. Use this when the host runs sandboxed (Mac Catalyst,
    /// iOS, the Windows App SDK packaged variant, Android) where the default
    /// <c>EndpointStorage</c> cannot persist <see cref="ClientInfo.ClientSettings"/>
    /// to the .app bundle's read-only assembly directory.
    /// </summary>
    /// <remarks>
    /// Hosts opt in by assigning <c>ClientInfo.EndpointStorage = new MauiPreferenceEndpointStorage();</c>
    /// inside <c>MauiProgram.CreateMauiApp</c>, before any code calls
    /// <see cref="ClientInfo.Initialize(string)"/> or <see cref="ClientInfo.SetEndpoint(string)"/>.
    /// Reading and writing happen through <see cref="Microsoft.Maui.Storage.Preferences.Default"/>,
    /// so the value persists across launches at the platform-recommended key store
    /// (NSUserDefaults on Apple, SharedPreferences on Android, the registry on Windows).
    /// </remarks>
    public sealed class MauiPreferenceEndpointStorage : IEndpointStorage
    {
        /// <summary>The key under which the endpoint is stored in MAUI <see cref="Preferences"/>.</summary>
        public const string PreferenceKey = "Bee.UI.Maui.Endpoint";

        /// <inheritdoc/>
        public string LoadEndpoint() => Microsoft.Maui.Storage.Preferences.Default.Get(PreferenceKey, string.Empty);

        /// <inheritdoc/>
        public void SetEndpoint(string endpoint) => Microsoft.Maui.Storage.Preferences.Default.Set(PreferenceKey, endpoint);

        /// <inheritdoc/>
        public void SaveEndpoint(string endpoint) => Microsoft.Maui.Storage.Preferences.Default.Set(PreferenceKey, endpoint);
    }
}
