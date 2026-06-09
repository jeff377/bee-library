using Bee.Api.Client;
using Bee.UI.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avalonia.Demo.ViewModels
{
    /// <summary>
    /// First step of the demo flow. Lets the user edit the JSON-RPC endpoint and
    /// runs a <c>system.ping</c> through <see cref="ClientInfo.Initialize(string)"/>;
    /// on success it invokes the <c>onConnected</c> callback supplied by the
    /// parent <see cref="MainWindowViewModel"/>.
    /// </summary>
    public partial class ConnectionViewModel : ViewModelBase
    {
        private readonly Action _onConnected;

        /// <summary>
        /// Service endpoint URL. Pre-filled with <see cref="Program.DefaultEndpoint"/>;
        /// the user can edit before hitting Connect.
        /// </summary>
        [ObservableProperty]
        private string _endpoint = Program.DefaultEndpoint;

        /// <summary>
        /// Status line text mirroring the ping / connect / error message.
        /// </summary>
        [ObservableProperty]
        private string _status = "Idle. Make sure samples/QuickStart.Server is running before connecting.";

        /// <summary>
        /// Indicates whether the latest <see cref="Status"/> message reflects an
        /// error (drives the foreground colour in the view).
        /// </summary>
        [ObservableProperty]
        private bool _isError;

        /// <summary>
        /// True while a ping / connect is in flight. Drives the progress indicator
        /// and disables the form inputs.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        /// <summary>
        /// Inverse of <see cref="IsBusy"/>; bound to <c>IsEnabled</c> on the form
        /// inputs so they grey out during a round-trip.
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Initialises a new instance with the parent's "advance to login" callback.
        /// </summary>
        /// <param name="onConnected">Invoked after a successful connect / ping.</param>
        public ConnectionViewModel(Action onConnected)
        {
            ArgumentNullException.ThrowIfNull(onConnected);
            _onConnected = onConnected;
        }

        /// <summary>
        /// Bound to the Connect button. Pings <see cref="Endpoint"/> via
        /// <see cref="ClientInfo.Initialize(string)"/> and advances on success.
        /// </summary>
        [RelayCommand]
        private async Task ConnectAsync()
        {
            if (IsBusy) return;

            var endpoint = Endpoint?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(endpoint))
            {
                SetStatus("Endpoint cannot be empty.", isError: true);
                return;
            }

            IsBusy = true;
            SetStatus($"Pinging {endpoint} …", isError: false);

            try
            {
                // ClientInfo.Initialize runs ApiConnectValidator (HTTP reachability +
                // ping) then stores the endpoint via EndpointStorage. Wrapped in
                // Task.Run because the call eventually does sync HTTP I/O; we don't
                // want to block the UI thread while waiting.
                await Task.Run(() => ClientInfo.Initialize(endpoint)).ConfigureAwait(true);
                ApiClientInfo.ApiKey = "avalonia-demo";

                SetStatus(
                    $"Connected to {endpoint}. ConnectType = {ApiClientInfo.ConnectType}.",
                    isError: false);
                _onConnected();
            }
            catch (Exception ex)
            {
                SetStatus($"Connect failed: {ex.Message}", isError: true);
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
