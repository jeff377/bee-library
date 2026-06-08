using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Bee.DefineEditor.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the dialog modally over <paramref name="owner"/> and returns
    /// <c>true</c> if the user clicked the confirm button, <c>false</c> if
    /// they cancelled or closed the window. Pass <paramref name="confirmLabel"/>
    /// to override the default "OK" button text (e.g. "Save anyway" or
    /// "Delete") — caller resolves the label through LocalizationService.
    /// </summary>
    public static async Task<bool> ShowAsync(
        Window owner, string title, string message,
        string? confirmLabel = null, string? cancelLabel = null)
    {
        var dialog = new ConfirmationDialog();
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.Title = title;
        if (!string.IsNullOrEmpty(confirmLabel)) dialog.ConfirmButton.Content = confirmLabel;
        if (!string.IsNullOrEmpty(cancelLabel)) dialog.CancelButton.Content = cancelLabel;
        return await dialog.ShowDialog<bool>(owner);
    }

    private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
