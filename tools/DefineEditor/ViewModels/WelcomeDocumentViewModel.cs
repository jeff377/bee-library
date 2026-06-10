using System.Collections.ObjectModel;
using Bee.DefineEditor.Services;
using Bee.DefineEditor.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// VS Code-style Welcome tab: quick-start actions, the recent-solutions list
/// (same source as File → Open Recent) and usage tips. Opened automatically at
/// startup when <see cref="UserSettings.ShowWelcomeOnStartup"/> is set, and on
/// demand via View → Welcome. Never dirty, never saveable — a plain closable
/// tab in every other respect.
/// </summary>
public sealed partial class WelcomeDocumentViewModel : DocumentViewModelBase
{
    /// <summary>
    /// Localized at construction; a later culture switch keeps the old tab
    /// title until the tab is reopened (Title has no change notification,
    /// matching the file-named tabs which never retitle either).
    /// </summary>
    public override string Title { get; } = L("Welcome_TabTitle");

    /// <summary>
    /// Empty — the welcome tab has no backing file. The shell's already-open
    /// matching compares against non-empty file paths only, and the status
    /// bar hides its path segment when this is empty.
    /// </summary>
    public override string DocumentKey => string.Empty;

    public override string TabIcon => "DefWelcome";

    public ObservableCollection<string> RecentSolutions { get; }

    public bool HasRecentSolutions => RecentSolutions.Count > 0;

    [ObservableProperty]
    private bool _showOnStartup;

    public WelcomeDocumentViewModel()
    {
        var settings = UserSettings.Load();
        RecentSolutions = new ObservableCollection<string>(settings.RecentSolutions);
        _showOnStartup = settings.ShowWelcomeOnStartup;
        StatusText = string.Empty;
    }

    partial void OnShowOnStartupChanged(bool value)
    {
        var settings = UserSettings.Load();
        settings.ShowWelcomeOnStartup = value;
        settings.Save();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822",
        Justification = "RelayCommand handler bound from XAML must remain instance.")]
    [RelayCommand]
    private void OpenFolder()
    {
        if (GetOwnerWindow() is MainWindow main)
            main.PromptOpenSolution();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822",
        Justification = "RelayCommand handler bound from XAML must remain instance.")]
    [RelayCommand]
    private void OpenRecent(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (GetOwnerWindow()?.DataContext is MainWindowViewModel vm)
            vm.OpenSolution(path);
    }
}
