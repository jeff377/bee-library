using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Bee.DefineEditor.Services;
using Bee.DefineEditor.ViewModels;

namespace Bee.DefineEditor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Pops the OS folder-picker and opens the chosen folder as a DefinePath
    /// solution. Public so the macOS NativeMenu (set up in
    /// <see cref="App.ConfigureWindowMenu"/>) can invoke it through a
    /// command — the picker needs this window's <see cref="StorageProvider"/>,
    /// which only Window-derived types expose.
    /// </summary>
    public async void PromptOpenSolution()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = LocalizationService.Current["Welcome_OpenFolderTip"],
            AllowMultiple = false,
        });

        if (folders.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            vm.OpenSolution(folders[0].Path.LocalPath);
        }
    }

    // Welcome-panel "Open Folder" button click handler.
    private void OnOpenSolutionClick(object? sender, RoutedEventArgs e) => PromptOpenSolution();
}
