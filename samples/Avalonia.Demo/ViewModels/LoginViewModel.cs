using Bee.UI.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Demo.ViewModels
{
    /// <summary>
    /// Second step of the demo flow. Calls
    /// <c>SystemApiConnector.LoginAsync</c> through
    /// <see cref="ClientInfo.SystemApiConnector"/> and stores the returned token
    /// via <see cref="ClientInfo.ApplyLoginResult"/>; on success it invokes the
    /// <c>onLoggedIn</c> callback supplied by the parent
    /// <see cref="MainWindowViewModel"/>.
    /// </summary>
    public partial class LoginViewModel : ViewModelBase
    {
        /// <summary>Default user id (matches the QuickStart demo seed).</summary>
        public const string DefaultUserId = "demo";

        /// <summary>Default password (matches the QuickStart demo seed).</summary>
        public const string DefaultPassword = "demo";

        private readonly Action _onLoggedIn;

        /// <summary>User identifier sent to <c>SystemApiConnector.LoginAsync</c>.</summary>
        [ObservableProperty]
        private string _userId = DefaultUserId;

        /// <summary>Password sent to <c>SystemApiConnector.LoginAsync</c>.</summary>
        [ObservableProperty]
        private string _password = DefaultPassword;

        /// <summary>Status line text mirroring the authentication outcome.</summary>
        [ObservableProperty]
        private string _status = "Use demo/demo against samples/QuickStart.Server.";

        /// <summary>Indicates whether <see cref="Status"/> is an error message.</summary>
        [ObservableProperty]
        private bool _isError;

        /// <summary>True while the login round-trip is in flight.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        /// <summary>Inverse of <see cref="IsBusy"/>; bound to input IsEnabled.</summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Initialises a new instance with the parent's "advance to employee" callback.
        /// </summary>
        /// <param name="onLoggedIn">Invoked after a successful authentication.</param>
        public LoginViewModel(Action onLoggedIn)
        {
            ArgumentNullException.ThrowIfNull(onLoggedIn);
            _onLoggedIn = onLoggedIn;
        }

        /// <summary>
        /// Bound to the Sign-in button.
        /// </summary>
        [RelayCommand]
        private async Task LoginAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            SetStatus("Authenticating …", isError: false);

            try
            {
                var connector = ClientInfo.SystemApiConnector;
                var response = await connector
                    .LoginAsync(UserId ?? string.Empty, Password ?? string.Empty)
                    .ConfigureAwait(true);
                ClientInfo.ApplyLoginResult(response);

                SetStatus($"Welcome, {response.UserName}.", isError: false);
                _onLoggedIn();
            }
            catch (Exception ex)
            {
                SetStatus($"Login failed: {ex.Message}", isError: true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SetStatus(string text, bool isError)
        {
            Status = text;
            IsError = isError;
        }
    }
}
