using System.Threading.Tasks;
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
    /// they cancelled or closed the window.
    /// </summary>
    public static async Task<bool> ShowAsync(Window owner, string title, string message)
    {
        var dialog = new ConfirmationDialog();
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.Title = title;
        return await dialog.ShowDialog<bool>(owner);
    }

    private void OnConfirm(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
