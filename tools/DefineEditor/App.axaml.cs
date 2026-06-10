using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Bee.DefineEditor.Services;
using Bee.DefineEditor.ViewModels;
using Bee.DefineEditor.Views;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor;

public partial class App : Application
{
    public IRelayCommand AboutCommand { get; }
    public IRelayCommand HideCommand { get; }
    public IRelayCommand HideOthersCommand { get; }
    public IRelayCommand ShowAllCommand { get; }
    public IRelayCommand QuitCommand { get; }
    public IRelayCommand OpenSolutionCommand { get; }
    public IRelayCommand ToggleThemeCommand { get; }
    public IRelayCommand WelcomeCommand { get; }

    // Proxies that forward File menu Save / Validate to whichever document is
    // currently active. CanExecute is re-evaluated each time ActiveDocument
    // changes (see OnFrameworkInitializationCompleted) so the menu items grey
    // out when no document is open or when the active one doesn't support
    // saving (e.g. UnsupportedDocumentViewModel).
    public IRelayCommand SaveActiveCommand { get; }
    public IRelayCommand SaveAllCommand { get; }
    public IRelayCommand ValidateActiveCommand { get; }
    public IRelayCommand CloseActiveTabCommand { get; }

    /// <summary>
    /// Short product name shown as the macOS application-menu title and inside
    /// the About / Hide / Quit item labels. VS Code shows "Code", not its full
    /// product name — same idea; the About dialog keeps the full name.
    /// </summary>
    private const string AppMenuName = "DefineEditor";

    /// <summary>
    /// File → Open Recent submenu, rebuilt from <see cref="UserSettings.RecentSolutions"/>
    /// whenever a solution opens, the list is cleared, or the UI culture changes.
    /// </summary>
    private readonly NativeMenu _recentMenu = new();

    public App()
    {
        AboutCommand = new RelayCommand(ShowAbout);
        HideCommand = new RelayCommand(HideApp);
        HideOthersCommand = new RelayCommand(HideOtherApps);
        ShowAllCommand = new RelayCommand(ShowAllApps);
        QuitCommand = new RelayCommand(Quit);
        OpenSolutionCommand = new RelayCommand(PromptOpenSolution);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        WelcomeCommand = new RelayCommand(() => GetMainViewModel()?.ShowWelcome());
        SaveActiveCommand = new RelayCommand(ExecuteActiveSave, CanExecuteActiveSave);
        SaveAllCommand = new RelayCommand(ExecuteSaveAll, CanExecuteSaveAll);
        ValidateActiveCommand = new RelayCommand(ExecuteActiveValidate, CanExecuteActiveValidate);
        CloseActiveTabCommand = new RelayCommand(ExecuteCloseActiveTab, CanExecuteCloseActiveTab);
    }

    public override void Initialize()
    {
        // macOS 主選單列的應用程式名（NSApplication.AppName）。短名慣例同
        // VS Code 的 "Code"；About 對話框內文仍用完整名稱 Bee.DefineEditor。
        Name = AppMenuName;
        AvaloniaXamlLoader.Load(this);

        // Load the persisted UI language (defaults to English) before any
        // window/menu builds — XAML's {loc:Loc ...} bindings read through
        // LocalizationService.Current immediately, so the very first paint
        // already shows the right language.
        var settings = UserSettings.Load();
        LocalizationService.Current.Culture = CultureInfo.GetCultureInfo(settings.Language);

        // App-level NativeMenu = the macOS application menu (the bold first
        // entry on the menu bar named after the app). Items added directly
        // here fold into that menu. Set before framework init so Avalonia's
        // default exporter doesn't inject "About Avalonia" first.
        ConfigureApplicationMenu();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainWindowViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = vm,
            };
            desktop.MainWindow = mainWindow;

            // VS Code-style Welcome tab on startup; opt-out via the checkbox
            // on the page itself, reachable any time through View → Welcome.
            if (UserSettings.Load().ShowWelcomeOnStartup)
                vm.ShowWelcome();

            // Re-evaluate the File menu's Save / Validate availability when
            // the active tab changes. Without this the menu items stay
            // disabled (or stale-enabled) after switching documents because
            // RelayCommand only re-checks CanExecute when we tell it to.
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.ActiveDocument))
                {
                    ((RelayCommand)SaveActiveCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)ValidateActiveCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)CloseActiveTabCommand).NotifyCanExecuteChanged();
                }
                else if (e.PropertyName == nameof(MainWindowViewModel.HasDirtyDocuments))
                {
                    ((RelayCommand)SaveAllCommand).NotifyCanExecuteChanged();
                }
                else if (e.PropertyName == nameof(MainWindowViewModel.SolutionPath))
                {
                    // OpenSolution touched UserSettings.RecentSolutions just
                    // before assigning SolutionPath — refresh the submenu.
                    RebuildRecentMenu();
                }
            };

            // The "Clear Recent" label inside the dynamically rebuilt submenu
            // doesn't go through LocItem (rebuilding would pile up culture
            // subscriptions), so refresh the whole submenu on culture switch.
            LocalizationService.Current.CultureChanged += (_, _) => RebuildRecentMenu();

            // Window-level NativeMenu = the additional top-level menus that
            // sit to the right of the App menu (File / View / ...). Setting
            // these on the Window — not the Application — is what makes
            // macOS treat each sub-menu as a separate top-level menu bar
            // entry instead of folding them into the App menu.
            ConfigureWindowMenu(mainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Builds the macOS application menu manually. <c>Program.cs</c> sets
    /// <see cref="MacOSPlatformOptions.DisableDefaultApplicationMenuItems"/>=true,
    /// so Avalonia no longer adds "About Avalonia" / "Quit" itself — we provide
    /// the full app menu (About / Hide / Quit) with our own About wired to
    /// <see cref="AboutDialog"/>.
    /// </summary>
    private void ConfigureApplicationMenu()
    {
        // Items added directly to the root NativeMenu (no sub-Menu) are folded
        // into the macOS application menu — the bold first entry named after
        // the app. Program.cs disables Avalonia's default app menu items so
        // these are the only contents.
        // Standard macOS app-menu shape: About / sep / Hide, Hide Others,
        // Show All / sep / Quit.
        var menu = new NativeMenu();
        menu.Add(LocItem("MenuItem_AboutApp", AboutCommand, gesture: null, AppMenuName));
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(LocItem("MenuItem_HideApp", HideCommand, new KeyGesture(Key.H, KeyModifiers.Meta), AppMenuName));
        menu.Add(LocItem("MenuItem_HideOthers", HideOthersCommand, new KeyGesture(Key.H, KeyModifiers.Meta | KeyModifiers.Alt)));
        menu.Add(LocItem("MenuItem_ShowAll", ShowAllCommand, gesture: null));
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(LocItem("MenuItem_QuitApp", QuitCommand, new KeyGesture(Key.Q, KeyModifiers.Meta), AppMenuName));
        NativeMenu.SetMenu(this, menu);
    }

    private void ConfigureWindowMenu(Window owner)
    {
        // Window-level NativeMenu produces the additional top-level menus
        // that appear to the right of the App menu (File / View / ...).
        // On Windows / Linux it's not rendered; the welcome panel's
        // "Open Folder" button serves as the fallback entry point there.
        var menu = new NativeMenu();

        // ── File menu ───────────────────────────────────────────────
        var fileMenu = LocItem("Menu_File", command: null, gesture: null);
        fileMenu.Menu = new NativeMenu();
        // VS Code's macOS shortcut for "Open Folder..." is Cmd+Shift+O; reused.
        fileMenu.Menu.Add(LocItem("MenuItem_OpenFolder", OpenSolutionCommand,
            new KeyGesture(Key.O, KeyModifiers.Meta | KeyModifiers.Shift)));
        var openRecent = LocItem("MenuItem_OpenRecent", command: null, gesture: null);
        openRecent.Menu = _recentMenu;
        fileMenu.Menu.Add(openRecent);
        RebuildRecentMenu();
        fileMenu.Menu.Add(new NativeMenuItemSeparator());
        fileMenu.Menu.Add(LocItem("MenuItem_Save", SaveActiveCommand,
            new KeyGesture(Key.S, KeyModifiers.Meta)));
        // VS Code's macOS shortcut for "Save All" is Cmd+Option+S; reused.
        fileMenu.Menu.Add(LocItem("MenuItem_SaveAll", SaveAllCommand,
            new KeyGesture(Key.S, KeyModifiers.Meta | KeyModifiers.Alt)));
        // VS Code uses Cmd+Shift+B for build / validate-style commands; reused.
        fileMenu.Menu.Add(LocItem("MenuItem_Validate", ValidateActiveCommand,
            new KeyGesture(Key.B, KeyModifiers.Meta | KeyModifiers.Shift)));
        fileMenu.Menu.Add(new NativeMenuItemSeparator());
        fileMenu.Menu.Add(LocItem("MenuItem_CloseActiveTab", CloseActiveTabCommand,
            new KeyGesture(Key.W, KeyModifiers.Meta)));
        menu.Add(fileMenu);

        // ── View menu ───────────────────────────────────────────────
        var viewMenu = LocItem("Menu_View", command: null, gesture: null);
        viewMenu.Menu = new NativeMenu();
        viewMenu.Menu.Add(LocItem("MenuItem_Welcome", WelcomeCommand, gesture: null));
        viewMenu.Menu.Add(new NativeMenuItemSeparator());
        viewMenu.Menu.Add(LocItem("MenuItem_ToggleTheme", ToggleThemeCommand, gesture: null));
        viewMenu.Menu.Add(new NativeMenuItemSeparator());

        // Language sub-menu. Each item flips LocalizationService.Culture and
        // persists the choice; XAML bindings via {loc:Loc} pick it up live.
        // The two language item labels stay in their own languages by design
        // (English / 繁體中文) — they identify themselves, not the current UI.
        var langMenu = LocItem("Menu_Language", command: null, gesture: null);
        langMenu.Menu = new NativeMenu();
        langMenu.Menu.Add(new NativeMenuItem("English")
        {
            Command = new RelayCommand(() => SetLanguage("en")),
        });
        langMenu.Menu.Add(new NativeMenuItem("繁體中文")
        {
            Command = new RelayCommand(() => SetLanguage("zh-TW")),
        });
        viewMenu.Menu.Add(langMenu);

        menu.Add(viewMenu);

        NativeMenu.SetMenu(owner, menu);
    }

    /// <summary>
    /// Creates a <see cref="NativeMenuItem"/> whose <c>Header</c> tracks the
    /// localized value for <paramref name="key"/>. Each item subscribes to
    /// <see cref="LocalizationService.CultureChanged"/> directly and re-writes
    /// its Header in place.
    /// </summary>
    /// <remarks>
    /// Note: macOS does <b>not</b> reflect Header updates on top-level
    /// (sub-menu-bearing) <see cref="NativeMenuItem"/> instances after the
    /// menu bar is measured — those titles stay frozen until app restart.
    /// We tried rebuilding the whole menu on culture switch, but
    /// <c>NativeMenu.SetMenu</c> over an already-mounted target crashes the
    /// process. So leaf items (Save / Validate / About / Hide / Quit / ...)
    /// update live; the three top-level headers (File / View / Language)
    /// stay in English. Acceptable trade-off for a dev tool — macOS apps
    /// commonly ship English-only menu headers anyway.
    /// </remarks>
    private static NativeMenuItem LocItem(
        string key,
        System.Windows.Input.ICommand? command,
        KeyGesture? gesture,
        params object[] formatArgs)
    {
        var item = new NativeMenuItem();

        void Refresh()
        {
            var raw = LocalizationService.Current[key];
            item.Header = formatArgs.Length == 0
                ? raw
                : string.Format(CultureInfo.InvariantCulture, raw, formatArgs);
        }

        Refresh();
        LocalizationService.Current.CultureChanged += (_, _) => Refresh();

        if (command is not null) item.Command = command;
        if (gesture is not null) item.Gesture = gesture;
        return item;
    }

    private static void SetLanguage(string cultureName)
    {
        LocalizationService.Current.Culture = CultureInfo.GetCultureInfo(cultureName);
        var settings = UserSettings.Load();
        settings.Language = cultureName;
        settings.Save();
        // No window re-creation needed — Markup.LocExtension now returns an
        // IObservable<string>.ToBinding(), which Avalonia genuinely re-pushes
        // to every bound target whenever the producer emits, including for
        // bindings inside DataTemplate-instantiated tab content.
    }

    private static void ShowAbout()
    {
        var dialog = new AboutDialog();
        var owner = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is not null)
            dialog.ShowDialog(owner);
        else
            dialog.Show();
    }

    /// <summary>
    /// True macOS app hide (NSApplication <c>hide:</c>) — windows vanish
    /// without the minimize animation and come back via Show All / Dock, the
    /// platform-standard ⌘H semantics. Off macOS the native menu isn't
    /// rendered, but keep a minimize fallback so the command stays sane if it
    /// ever gets another entry point.
    /// </summary>
    private static void HideApp()
    {
        if (OperatingSystem.IsMacOS())
        {
            MacNativeApp.Hide();
            return;
        }
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            foreach (var w in lifetime.Windows)
                w.WindowState = WindowState.Minimized;
    }

    private static void HideOtherApps()
    {
        if (OperatingSystem.IsMacOS())
            MacNativeApp.HideOthers();
    }

    private static void ShowAllApps()
    {
        if (OperatingSystem.IsMacOS())
            MacNativeApp.ShowAll();
    }

    private static void Quit()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
    }

    /// <summary>
    /// Currently active document, if any. Resolved lazily because the
    /// MainWindow / its DataContext may not exist yet when the App ctor builds
    /// the command list, and to avoid holding stale references when the
    /// lifetime swaps windows.
    /// </summary>
    private static DocumentViewModelBase? GetActiveDocument() => GetMainViewModel()?.ActiveDocument;

    private static bool CanExecuteActiveSave() =>
        GetActiveDocument()?.FileSaveCommand?.CanExecute(null) ?? false;

    private static void ExecuteActiveSave() =>
        GetActiveDocument()?.FileSaveCommand?.Execute(null);

    private static bool CanExecuteActiveValidate() =>
        GetActiveDocument()?.FileValidateCommand?.CanExecute(null) ?? false;

    private static void ExecuteActiveValidate() =>
        GetActiveDocument()?.FileValidateCommand?.Execute(null);

    /// <summary>
    /// Repopulates the File → Open Recent submenu from the persisted recent
    /// list: one item per solution path (most recent first), then a separator
    /// and "Clear Recent" (disabled while the list is empty). Items are plain
    /// <see cref="NativeMenuItem"/>s rebuilt wholesale, so no per-item culture
    /// subscriptions accumulate.
    /// </summary>
    private void RebuildRecentMenu()
    {
        _recentMenu.Items.Clear();

        var recents = UserSettings.Load().RecentSolutions;
        foreach (var path in recents)
        {
            var captured = path;
            _recentMenu.Add(new NativeMenuItem(path)
            {
                Command = new RelayCommand(() => OpenRecentSolution(captured)),
            });
        }

        if (recents.Count > 0)
            _recentMenu.Add(new NativeMenuItemSeparator());
        _recentMenu.Add(new NativeMenuItem(LocalizationService.Current["MenuItem_ClearRecent"])
        {
            Command = new RelayCommand(ClearRecentSolutions),
            IsEnabled = recents.Count > 0,
        });
    }

    private static void OpenRecentSolution(string path) =>
        GetMainViewModel()?.OpenSolution(path);

    private void ClearRecentSolutions()
    {
        var settings = UserSettings.Load();
        settings.RecentSolutions.Clear();
        settings.Save();
        RebuildRecentMenu();
    }

    private static bool CanExecuteCloseActiveTab() => GetActiveDocument() is not null;

    private static void ExecuteCloseActiveTab()
    {
        var vm = GetMainViewModel();
        if (vm?.ActiveDocument is { } doc)
            vm.CloseDocumentCommand.Execute(doc);
    }

    private static MainWindowViewModel? GetMainViewModel() =>
        (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
            ?.DataContext as MainWindowViewModel;

    private static bool CanExecuteSaveAll() =>
        GetMainViewModel()?.HasDirtyDocuments ?? false;

    private static void ExecuteSaveAll() =>
        GetMainViewModel()?.SaveAllCommand.Execute(null);

    /// <summary>
    /// Forwards "Open Folder..." from the macOS menu to <see cref="MainWindow"/>
    /// — the picker must run against a Window's <see cref="StorageProvider"/>,
    /// not at App level.
    /// </summary>
    private static void PromptOpenSolution()
    {
        if ((Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
            is MainWindow main)
        {
            main.PromptOpenSolution();
        }
    }

    /// <summary>
    /// Toggles the application-wide theme between Dark and Light. Bound to the
    /// "Toggle Theme" item under View in the macOS menu and to
    /// <see cref="MainWindowViewModel.ToggleThemeCommand"/>.
    /// </summary>
    public static void ToggleTheme()
    {
        if (Current is null) return;
        Current.RequestedThemeVariant =
            Current.ActualThemeVariant == ThemeVariant.Light
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
    }
}
