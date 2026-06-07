using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Bee.DefineEditor.ViewModels;

namespace Bee.DefineEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenSolutionClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "選擇 DefinePath 方案資料夾",
            AllowMultiple = false,
        });

        if (folders.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            vm.OpenSolution(folders[0].Path.LocalPath);
        }
    }
}
