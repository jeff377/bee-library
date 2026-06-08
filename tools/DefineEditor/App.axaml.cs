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
    public IRelayCommand QuitCommand { get; }
    public IRelayCommand OpenSolutionCommand { get; }
    public IRelayCommand ToggleThemeCommand { get; }

    // Proxies that forward File menu Save / Validate to whichever document is
    // currently active. CanExecute is re-evaluated each time ActiveDocument
    // changes (see OnFrameworkInitializationCompleted) so the menu items grey
    // out when no document is open or when the active one doesn't support
    // saving (e.g. UnsupportedDocumentViewModel).
    public IRelayCommand SaveActiveCommand { get; }
    public IRelayCommand ValidateActiveCommand { get; }

    public App()
    {
        AboutCommand = new RelayCommand(ShowAbout);
        HideCommand = new RelayCommand(HideAllWindows);
        QuitCommand = new RelayCommand(Quit);
        OpenSolutionCommand = new RelayCommand(PromptOpenSolution);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        SaveActiveCommand = new RelayCommand(ExecuteActiveSave, CanExecuteActiveSave);
        ValidateActiveCommand = new RelayCommand(ExecuteActiveValidate, CanExecuteActiveValidate);
    }

    public override void Initialize()
    {
        // macOS 主選單列的應用程式名（NSApplication.AppName）。
        Name = "Bee.DefineEditor";
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
                }
            };

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
        var menu = new NativeMenu();
        menu.Add(new NativeMenuItem("About Bee.DefineEditor")
        {
            Command = AboutCommand,
        });
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(new NativeMenuItem("Hide Bee.DefineEditor")
        {
            Command = HideCommand,
            Gesture = new KeyGesture(Key.H, KeyModifiers.Meta),
        });
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(new NativeMenuItem("Quit Bee.DefineEditor")
        {
            Command = QuitCommand,
            Gesture = new KeyGesture(Key.Q, KeyModifiers.Meta),
        });
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
        var fileMenu = new NativeMenuItem("File") { Menu = new NativeMenu() };
        fileMenu.Menu.Add(new NativeMenuItem("Open Folder...")
        {
            Command = OpenSolutionCommand,
            // VS Code's macOS shortcut for "Open Folder..." is Cmd+Shift+O;
            // matching it lets users move between the two with no muscle re-learn.
            Gesture = new KeyGesture(Key.O, KeyModifiers.Meta | KeyModifiers.Shift),
        });
        fileMenu.Menu.Add(new NativeMenuItemSeparator());
        fileMenu.Menu.Add(new NativeMenuItem("Save")
        {
            Command = SaveActiveCommand,
            Gesture = new KeyGesture(Key.S, KeyModifiers.Meta),
        });
        fileMenu.Menu.Add(new NativeMenuItem("Validate")
        {
            Command = ValidateActiveCommand,
            // VS Code uses Cmd+Shift+B for build / validate-style commands; reused.
            Gesture = new KeyGesture(Key.B, KeyModifiers.Meta | KeyModifiers.Shift),
        });
        menu.Add(fileMenu);

        // ── View menu ───────────────────────────────────────────────
        var viewMenu = new NativeMenuItem("View") { Menu = new NativeMenu() };
        viewMenu.Menu.Add(new NativeMenuItem("Toggle Theme")
        {
            Command = ToggleThemeCommand,
        });
        viewMenu.Menu.Add(new NativeMenuItemSeparator());

        // Language sub-menu. Each item flips LocalizationService.Culture and
        // persists the choice; XAML bindings via {loc:Loc} pick it up live.
        var langMenu = new NativeMenuItem("Language") { Menu = new NativeMenu() };
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

    private static void HideAllWindows()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            foreach (var w in lifetime.Windows)
                w.WindowState = WindowState.Minimized;
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
    private static DocumentViewModelBase? GetActiveDocument()
    {
        if ((Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
            is { DataContext: MainWindowViewModel vm })
        {
            return vm.ActiveDocument;
        }
        return null;
    }

    private static bool CanExecuteActiveSave() =>
        GetActiveDocument()?.FileSaveCommand?.CanExecute(null) ?? false;

    private static void ExecuteActiveSave() =>
        GetActiveDocument()?.FileSaveCommand?.Execute(null);

    private static bool CanExecuteActiveValidate() =>
        GetActiveDocument()?.FileValidateCommand?.CanExecute(null) ?? false;

    private static void ExecuteActiveValidate() =>
        GetActiveDocument()?.FileValidateCommand?.Execute(null);

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
