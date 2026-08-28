using Bee.UI.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.Northwind.UI.ViewModels;

/// <summary>
/// Second step of the demo flow. Signs in and then enters the demo company, storing both results
/// on <see cref="ClientInfo"/>; on success it invokes the <c>onLoggedIn</c> callback supplied by
/// the parent <see cref="MainWindowViewModel"/>.
/// </summary>
/// <remarks>
/// Sign-in is two calls, not one, and the demo makes both even though it has a single company:
/// <c>Login</c> answers who the caller is, <c>EnterCompany</c> answers which company they are in,
/// and the second one is what fills the company-scoped half of the session. Skipping it leaves a
/// token that authenticates but cannot open any <c>CategoryId="company"</c> form.
/// <para>
/// The company is entered automatically rather than picked, because there is only one. A
/// deployment with several puts a chooser between the two calls; nothing else about this changes.
/// </para>
/// </remarks>
public partial class LoginViewModel : ViewModelBase
{
    /// <summary>Default user id (matches the row the server's seeder writes into <c>st_user</c>).</summary>
    public const string DefaultUserId = "demo";

    /// <summary>Default password (matches the row the server's seeder writes into <c>st_user</c>).</summary>
    public const string DefaultPassword = "demo";

    /// <summary>
    /// The company entered after sign-in (matches the row the server's seeder writes into
    /// <c>st_company</c>).
    /// </summary>
    /// <remarks>
    /// Spelled out here rather than shared with the server's <c>NorthwindCredentials</c>: this
    /// project is a remote JSON-RPC client and references no server assembly, which is the same
    /// reason the two constants above are repeated.
    /// </remarks>
    public const string DemoCompanyId = "NORTHWIND";

    private readonly Action _onLoggedIn;

    /// <summary>User identifier sent to <c>SystemApiConnector.LoginAsync</c>.</summary>
    [ObservableProperty]
    private string _userId = DefaultUserId;

    /// <summary>Password sent to <c>SystemApiConnector.LoginAsync</c>.</summary>
    [ObservableProperty]
    private string _password = DefaultPassword;

    /// <summary>Status line text mirroring the authentication outcome.</summary>
    [ObservableProperty]
    private string _status = "Use demo/demo to sign in.";

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
    /// Initialises a new instance with the parent's "advance to forms" callback.
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

            // Second half of sign-in. ApplyEnterCompanyResult also flushes the definition cache,
            // which is why the company is entered before any form is opened rather than lazily on
            // the first one that needs it.
            //
            // WARNING: the connector is read again rather than reusing the local above. Storing the
            // token discards the cached connector, because that one was built around the token the
            // client held before signing in — an empty one here. Calling EnterCompany on the stale
            // instance fails with "AccessToken is required or invalid", which reads like a session
            // problem rather than a stale local.
            var company = await ClientInfo.SystemApiConnector
                .EnterCompanyAsync(DemoCompanyId)
                .ConfigureAwait(true);
            ClientInfo.ApplyEnterCompanyResult(company);

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
