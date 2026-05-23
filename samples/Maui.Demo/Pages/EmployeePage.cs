using Bee.UI.Core;
using Bee.UI.Maui.Controls;

namespace Maui.Demo.Pages;

/// <summary>
/// Hosts <see cref="FormPage"/> for the <c>Employee</c> program. Sets only
/// <see cref="FormPage.ProgId"/> and relies on the Phase 1d fallback: the page
/// pulls the <see cref="Bee.Definition.Forms.FormSchema"/> through
/// <see cref="ClientInfo.SystemApiConnector"/>, builds a connector through
/// <see cref="ClientInfo.CreateFormApiConnector(string)"/>, and inherits the
/// <see cref="ClientInfo.AccessToken"/> set by <see cref="LoginPage"/>.
/// </summary>
public sealed class EmployeePage : ContentPage
{
    private readonly FormPage _formPage;
    private readonly Label _errorLabel;

    /// <summary>Initializes the layout and wires the FormPage error event.</summary>
    public EmployeePage()
    {
        Title = "Employee";

        _formPage = new FormPage { ProgId = "Employee" };
        _formPage.ErrorOccurred += (_, ex) => DisplayError(ex);

        _errorLabel = new Label
        {
            TextColor = Colors.Firebrick,
            IsVisible = false,
        };

        Content = new ScrollView
        {
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children = { _errorLabel, _formPage },
            },
        };
    }

    private void DisplayError(Exception ex)
    {
        _errorLabel.Text = $"Backend error: {ex.Message}";
        _errorLabel.IsVisible = true;
    }
}
