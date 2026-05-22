using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages.System;
using Bee.Web.Blazor.Wasm.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Wasm.Components
{
    /// <summary>
    /// Minimal user-id / password panel that calls
    /// <see cref="SystemApiConnector.LoginAsync"/> and raises
    /// <see cref="OnLoggedIn"/> with the resulting <see cref="LoginResponse"/>.
    /// Pair it with <see cref="BeeAccessTokenProvider"/> to keep the access
    /// token cascading through the component tree.
    /// </summary>
    /// <remarks>
    /// The panel ships with a no-frills default look so it can drop in unstyled;
    /// hosts that want richer UI should provide their own login page and call
    /// <see cref="BeeAccessTokenProvider.SetToken"/> directly. Validation is
    /// also intentionally minimal — the backend BO ultimately decides what
    /// counts as a valid credential, and surfacing its rejection through the
    /// inline error message is enough for the sample-grade scope of Phase 1d.
    /// </remarks>
    public partial class BeeLoginPanel : ComponentBase
    {
        private readonly string _userIdInputId = $"bee-login-user-{Guid.NewGuid():N}";
        private readonly string _passwordInputId = $"bee-login-pwd-{Guid.NewGuid():N}";

        private string _userId = string.Empty;
        private string _password = string.Empty;
        private bool _isBusy;
        private string? _error;

        /// <summary>
        /// Gets or sets the label rendered above the user-id input.
        /// </summary>
        [Parameter]
        public string UserIdLabel { get; set; } = "User ID";

        /// <summary>
        /// Gets or sets the label rendered above the password input.
        /// </summary>
        [Parameter]
        public string PasswordLabel { get; set; } = "Password";

        /// <summary>
        /// Gets or sets the caption shown on the submit button.
        /// </summary>
        [Parameter]
        public string SubmitLabel { get; set; } = "Sign in";

        /// <summary>
        /// Gets or sets the callback invoked after a successful login. The
        /// host typically wires this to
        /// <c>response =&gt; auth.SetToken(response.AccessToken)</c> against
        /// an enclosing <see cref="BeeAccessTokenProvider"/>.
        /// </summary>
        [Parameter]
        public EventCallback<LoginResponse> OnLoggedIn { get; set; }

        [Inject]
        private BeeApiConnectorFactory Factory { get; set; } = default!;

        private async Task OnSubmitAsync()
        {
            if (_isBusy) return;
            _isBusy = true;
            _error = null;
            try
            {
                var system = Factory.CreateSystemConnector(Guid.Empty);
                var response = await system.LoginAsync(_userId, _password).ConfigureAwait(true);

                if (response.AccessToken == Guid.Empty)
                {
                    _error = "Login failed: the server returned an empty access token.";
                    return;
                }

                _password = string.Empty;

                if (OnLoggedIn.HasDelegate)
                {
                    await OnLoggedIn.InvokeAsync(response).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                // Surface the failure inline rather than letting it tear down
                // the WebAssembly app. The backend already classifies the
                // error (bad credentials, locked account, network) into the
                // exception message, which is what the user sees.
                _error = ex.Message;
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
