using Bee.UI.Core;

namespace Maui.Demo.Pages;

/// <summary>
/// Two-field login that calls <c>SystemApiConnector.LoginAsync</c> through
/// <see cref="ClientInfo.SystemApiConnector"/> and stores the returned token via
/// <see cref="ClientInfo.ApplyLoginResult"/>. Defaults to the demo credentials
/// (<c>demo</c> / <c>demo</c>) baked into <c>DemoAuthenticatingSystemBusinessObject</c>
/// on the server side.
/// </summary>
public sealed class LoginPage : ContentPage
{
    /// <summary>Demo user id (matches <c>Bee.Samples.Shared.DemoCredentials.UserId</c>).</summary>
    public const string DefaultUserId = "demo";

    /// <summary>Demo password (matches <c>Bee.Samples.Shared.DemoCredentials.Password</c>).</summary>
    public const string DefaultPassword = "demo";

    private readonly Entry _userIdEntry;
    private readonly Entry _passwordEntry;
    private readonly Button _loginButton;
    private readonly Label _statusLabel;
    private readonly ActivityIndicator _busyIndicator;

    /// <summary>Initializes the layout (userId + password entries + Login button).</summary>
    public LoginPage()
    {
        Title = "Login";

        _userIdEntry = new Entry { Text = DefaultUserId, Placeholder = "User ID" };
        _passwordEntry = new Entry
        {
            Text = DefaultPassword,
            Placeholder = "Password",
            IsPassword = true,
        };

        _loginButton = new Button
        {
            Text = "Sign in",
            HorizontalOptions = LayoutOptions.Start,
        };
        _loginButton.Clicked += async (_, _) => await OnLoginClickedAsync().ConfigureAwait(true);

        _statusLabel = new Label { TextColor = Colors.Gray, Text = "Use demo/demo against samples/QuickStart.Server." };
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
                        Text = "Sign in",
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                    },
                    new Label { Text = "User ID" },
                    _userIdEntry,
                    new Label { Text = "Password" },
                    _passwordEntry,
                    new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Children = { _loginButton, _busyIndicator },
                    },
                    _statusLabel,
                },
            },
        };
    }

    private async Task OnLoginClickedAsync()
    {
        SetBusy(true);
        SetStatus("Authenticating …", isError: false);

        try
        {
            var connector = ClientInfo.SystemApiConnector;
            var response = await connector
                .LoginAsync(_userIdEntry.Text ?? string.Empty, _passwordEntry.Text ?? string.Empty)
                .ConfigureAwait(true);
            ClientInfo.ApplyLoginResult(response);
            SetStatus($"Welcome, {response.UserName}.", isError: false);
            await Shell.Current.GoToAsync(nameof(EmployeePage)).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus($"Login failed: {ex.Message}", isError: true);
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
        _loginButton.IsEnabled = !busy;
        _userIdEntry.IsEnabled = !busy;
        _passwordEntry.IsEnabled = !busy;
    }

    private void SetStatus(string text, bool isError)
    {
        _statusLabel.Text = text;
        _statusLabel.TextColor = isError ? Colors.Firebrick : Colors.DarkSlateGray;
    }
}
