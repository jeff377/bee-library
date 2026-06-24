using Bee.Api.Client;
using Bee.UI.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.Northwind.UI.ViewModels;

/// <summary>
/// First step of the demo flow. Lets the user edit the JSON-RPC endpoint and runs a
/// <c>system.ping</c> through <see cref="ClientInfo.InitializeAsync(string)"/>; on success it
/// invokes the <c>onConnected</c> callback supplied by the parent
/// <see cref="MainWindowViewModel"/>.
/// </summary>
public partial class ConnectionViewModel : ViewModelBase
{
    private readonly Action _onConnected;

    /// <summary>
    /// Service endpoint URL. Pre-filled with <see cref="AppDefaults.Endpoint"/>; the user
    /// can edit before hitting Connect.
    /// </summary>
    [ObservableProperty]
    private string _endpoint = AppDefaults.Endpoint;

    /// <summary>Status line text mirroring the ping / connect / error message.</summary>
    [ObservableProperty]
    private string _status = "Idle. Make sure Bee.Northwind.Server is running before connecting.";

    /// <summary>
    /// Indicates whether the latest <see cref="Status"/> message reflects an error
    /// (drives the foreground colour in the view).
    /// </summary>
    [ObservableProperty]
    private bool _isError;

    /// <summary>
    /// True while a ping / connect is in flight. Drives the progress indicator and
    /// disables the form inputs.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    /// <summary>
    /// Inverse of <see cref="IsBusy"/>; bound to <c>IsEnabled</c> on the form inputs so
    /// they grey out during a round-trip.
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
    /// <see cref="ClientInfo.InitializeAsync(string)"/> and advances on success.
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
            // ClientInfo.InitializeAsync runs ApiConnectValidator (HTTP reachability + ping)
            // then stores the endpoint via EndpointStorage — fully async, so it does not block
            // the UI thread. The async path is required on browser WASM, whose single-threaded
            // runtime throws "Cannot wait on monitors" if any await is bridged synchronously.
            await ClientInfo.InitializeAsync(endpoint).ConfigureAwait(true);
            ApiClientInfo.ApiKey = AppDefaults.ApiKey;

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
