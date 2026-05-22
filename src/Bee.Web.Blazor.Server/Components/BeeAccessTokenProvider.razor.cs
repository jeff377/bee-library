using Bee.Api.Client.Connectors;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Server.Components
{
    /// <summary>
    /// Holds the in-circuit <c>AccessToken</c> and exposes it as a cascading
    /// value so descendant components (e.g. <see cref="FormPage"/>) can pick it
    /// up through <c>[CascadingParameter] public Guid AccessToken</c>.
    /// </summary>
    /// <remarks>
    /// Wrap a layout fragment with <c>&lt;BeeAccessTokenProvider Context="auth"&gt;</c>
    /// and call <see cref="SetToken"/> when the user signs in
    /// (e.g. from <see cref="BeeLoginPanel"/>'s <c>OnLoggedIn</c> callback):
    /// the provider re-renders, propagates the new value via the inner
    /// <c>CascadingValue&lt;Guid&gt;</c>, and authenticated children re-fetch.
    /// State lives in memory only — Blazor Server scopes it to the SignalR
    /// circuit; Blazor WASM scopes it to the component instance. Persistence
    /// across reconnect / refresh (ProtectedSessionStorage, sessionStorage,
    /// etc.) is deliberately out of scope for Phase 1d.
    /// </remarks>
    public partial class BeeAccessTokenProvider : ComponentBase
    {
        private bool _isAttached;

        /// <summary>
        /// Gets the current access token. <see cref="Guid.Empty"/> means
        /// anonymous; any other value indicates an authenticated session.
        /// </summary>
        public Guid AccessToken { get; private set; }

        /// <summary>
        /// Gets a value indicating whether <see cref="AccessToken"/> identifies
        /// an authenticated session (i.e. is not <see cref="Guid.Empty"/>).
        /// </summary>
        public bool IsAuthenticated => AccessToken != Guid.Empty;

        /// <summary>
        /// Gets or sets the templated child content. The provider passes
        /// <c>this</c> as the template context so callers can write
        /// <c>&lt;BeeAccessTokenProvider Context="auth"&gt;</c> and invoke
        /// <see cref="SetToken"/> / <see cref="Clear"/> from inside.
        /// </summary>
        [Parameter, EditorRequired]
        public RenderFragment<BeeAccessTokenProvider>? ChildContent { get; set; }

        /// <summary>
        /// Stores <paramref name="accessToken"/> and re-renders the provider so
        /// the cascading value reaches authenticated children. Pass the value
        /// returned by <see cref="SystemApiConnector.LoginAsync"/>.
        /// </summary>
        public void SetToken(Guid accessToken)
        {
            if (AccessToken == accessToken) return;
            AccessToken = accessToken;
            if (_isAttached) StateHasChanged();
        }

        /// <summary>
        /// Clears the stored token (logout). Equivalent to
        /// <c>SetToken(Guid.Empty)</c>.
        /// </summary>
        public void Clear() => SetToken(Guid.Empty);

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            // The renderer is attached by the time OnInitialized fires; before
            // that point calling `StateHasChanged` would throw, so the flag
            // gates the call for the benefit of isolated unit tests that
            // instantiate the component directly without a renderer.
            _isAttached = true;
        }
    }
}
