using Bee.Api.Client;
using Bee.UI.Core;

namespace Maui.Demo.Pages;

/// <summary>
/// First page the user sees. Lets them edit the JSON-RPC endpoint and run a
/// <c>system.ping</c> through <see cref="ClientInfo.InitializeAsync(string)"/>; on
/// success it pushes <see cref="LoginPage"/> onto the Shell stack.
/// </summary>
/// <remarks>
/// The page builds its layout in code rather than XAML to match the rest of the
/// Bee MAUI sample style (FormPage / DynamicForm / DynamicGrid are all
/// code-based). One file, no .xaml/.xaml.cs pair to keep in sync.
/// </remarks>
public sealed class ConnectionPage : ContentPage
{
    private readonly Entry _endpointEntry;
    private readonly Button _connectButton;
    private readonly Label _statusLabel;
    private readonly ActivityIndicator _busyIndicator;

    /// <summary>Initializes the layout (endpoint entry + Connect button + status line).</summary>
    public ConnectionPage()
    {
        Title = "Connect";

        _endpointEntry = new Entry
        {
            Text = MauiProgram.DefaultEndpoint,
            Placeholder = "http://host:port/api",
            Keyboard = Keyboard.Url,
        };

        _connectButton = new Button
        {
            Text = "Connect",
            HorizontalOptions = LayoutOptions.Start,
        };
        _connectButton.Clicked += async (_, _) => await OnConnectClickedAsync().ConfigureAwait(true);

        _statusLabel = new Label
        {
            TextColor = Colors.Gray,
            Text = "Idle. Make sure samples/QuickStart.Server is running before connecting.",
        };

        _busyIndicator = new ActivityIndicator { IsVisible = false };

        Content = new ScrollView
        {
            Padding = 24,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = "Bee MAUI demo",
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                    },
                    new Label
                    {
                        Text = "Point this app at a Bee JSON-RPC endpoint, log in with the demo credentials, then render the Employee FormSchema. The MAUI controls call the same backend the Blazor demos use.",
                        TextColor = Colors.Gray,
                    },
                    new Label { Text = "Endpoint" },
                    _endpointEntry,
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Children = { _connectButton, _busyIndicator },
                    },
                    _statusLabel,
                },
            },
        };
    }

    private async Task OnConnectClickedAsync()
    {
        var endpoint = _endpointEntry.Text?.Trim();
        if (string.IsNullOrEmpty(endpoint))
        {
            SetStatus("Endpoint cannot be empty.", isError: true);
            return;
        }

        SetBusy(true);
        SetStatus($"Pinging {endpoint} …", isError: false);

        try
        {
            // ClientInfo.InitializeAsync runs ApiConnectValidator (HTTP reachability + ping)
            // then stores the endpoint via EndpointStorage, awaiting the work instead of
            // blocking the UI thread.
            await ClientInfo.InitializeAsync(endpoint).ConfigureAwait(true);
            ApiClientInfo.ApiKey = "maui-demo";
            SetStatus($"Connected to {endpoint}. ConnectType = {ApiClientInfo.ConnectType}.", isError: false);
            await Shell.Current.GoToAsync(nameof(LoginPage)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus($"Connect failed: {ex.Message}", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busyIndicator.IsVisible = busy;
        _busyIndicator.IsRunning = busy;
        _connectButton.IsEnabled = !busy;
        _endpointEntry.IsEnabled = !busy;
    }

    private void SetStatus(string text, bool isError)
    {
        _statusLabel.Text = text;
        _statusLabel.TextColor = isError ? Colors.Firebrick : Colors.DarkSlateGray;
    }
}
