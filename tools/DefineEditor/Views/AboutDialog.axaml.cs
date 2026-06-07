using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Bee.DefineEditor.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        VersionText.Text = $"Version {GetVersion()}";
    }

    private static string GetVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(info)) return info;
        return asm.GetName().Version?.ToString() ?? "dev";
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}
