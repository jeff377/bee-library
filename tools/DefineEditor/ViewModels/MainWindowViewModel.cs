using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Services;
using Bee.Definition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<DefineNode> _nodes = new();

    /// <summary>
    /// Open document tabs. New tabs are appended; selecting an already-open
    /// node activates the existing tab rather than re-loading.
    /// </summary>
    public ObservableCollection<DocumentViewModelBase> OpenDocuments { get; } = new();

    [ObservableProperty]
    private DocumentViewModelBase? _activeDocument;

    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>
    /// Root path of the currently open solution. Empty when no solution is
    /// open — the welcome panel checks this to decide whether to show the
    /// "Open Folder" call-to-action vs the tree view.
    /// </summary>
    [ObservableProperty]
    private string _solutionPath = string.Empty;

    /// <summary>
    /// True when a solution is open. Derived from <see cref="SolutionPath"/>;
    /// bind <c>IsVisible</c> to this for "show tree" branches and to its
    /// negation for "show welcome" branches.
    /// </summary>
    public bool IsSolutionOpened => !string.IsNullOrEmpty(SolutionPath);

    [ObservableProperty]
    private DefineNode? _selectedNode;

    /// <summary>
    /// Solution-wide context (currently: the set of available FormSchema ProgIds)
    /// rebuilt each time a DefinePath is opened.
    /// </summary>
    public SolutionContext Solution { get; private set; } = SolutionContext.Empty;

    public bool HasOpenDocuments => OpenDocuments.Count > 0;

    public bool HasActiveDocument => ActiveDocument is not null;

    /// <summary>
    /// True when at least one open document has unsaved edits and supports
    /// saving. Drives the File menu's "Save All" availability; refreshed from
    /// each document's IsDirty change (see <see cref="OnOpenDocumentsChanged"/>).
    /// </summary>
    public bool HasDirtyDocuments =>
        OpenDocuments.Any(d => d.IsDirty && d.FileSaveCommand is not null);

    /// <summary>
    /// Active document's file path relative to <see cref="SolutionPath"/>, e.g.
    /// "FormSchema/Employee.xml". Empty when no document is active or the
    /// solution root is unknown. Bound to the status bar so the user can see
    /// which file they're editing at a glance — VS Code shows the same.
    /// </summary>
    public string ActiveDocumentRelativePath
    {
        get
        {
            if (ActiveDocument is null || string.IsNullOrEmpty(SolutionPath))
                return string.Empty;
            var key = ActiveDocument.DocumentKey;
            if (string.IsNullOrEmpty(key)) return string.Empty;
            try { return Path.GetRelativePath(SolutionPath, key); }
            catch (ArgumentException) { return key; }
        }
    }

    /// <summary>
    /// Visibility hint for the right-pane "select a node to open a tab" welcome.
    /// Shown only when a solution is open but no document tab is active —
    /// otherwise the left-pane "Open Folder" welcome covers the empty state.
    /// </summary>
    public bool ShowDocumentWelcome => IsSolutionOpened && !HasOpenDocuments;

    public MainWindowViewModel()
    {
        OpenDocuments.CollectionChanged += OnOpenDocumentsChanged;
    }

    /// <summary>
    /// Documents whose PropertyChanged we are currently subscribed to for
    /// IsDirty tracking. Re-synced wholesale on every collection change —
    /// tab counts are small, and full resync sidesteps the Reset-action
    /// case where the removed items are not reported.
    /// </summary>
    private readonly List<DocumentViewModelBase> _dirtySubscriptions = new();

    private void OnOpenDocumentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasOpenDocuments));
        OnPropertyChanged(nameof(ShowDocumentWelcome));

        foreach (var doc in _dirtySubscriptions)
            doc.PropertyChanged -= OnDocumentPropertyChanged;
        _dirtySubscriptions.Clear();
        foreach (var doc in OpenDocuments)
        {
            doc.PropertyChanged += OnDocumentPropertyChanged;
            _dirtySubscriptions.Add(doc);
        }
        OnPropertyChanged(nameof(HasDirtyDocuments));
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentViewModelBase.IsDirty))
            OnPropertyChanged(nameof(HasDirtyDocuments));
    }

    partial void OnSolutionPathChanged(string value)
    {
        OnPropertyChanged(nameof(IsSolutionOpened));
        OnPropertyChanged(nameof(ShowDocumentWelcome));
        OnPropertyChanged(nameof(ActiveDocumentRelativePath));
    }

    partial void OnActiveDocumentChanged(DocumentViewModelBase? value)
    {
        OnPropertyChanged(nameof(HasActiveDocument));
        OnPropertyChanged(nameof(ActiveDocumentRelativePath));
    }

    partial void OnSelectedNodeChanged(DefineNode? value)
    {
        if (value is null
            || value.Kind != DefineNodeKind.DefineFile
            || string.IsNullOrEmpty(value.FilePath))
        {
            return;
        }

        // Already open? Activate that tab instead of creating a duplicate.
        var existing = OpenDocuments.FirstOrDefault(d =>
            string.Equals(d.DocumentKey, value.FilePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            ActiveDocument = existing;
            return;
        }

        var doc = DocumentViewModelFactory.Create(value, Solution);
        if (doc is null) return;
        OpenDocuments.Add(doc);
        ActiveDocument = doc;
    }

    [RelayCommand]
    private void CloseDocument(DocumentViewModelBase? doc)
    {
        if (doc is null) return;
        var idx = OpenDocuments.IndexOf(doc);
        if (idx < 0) return;

        OpenDocuments.RemoveAt(idx);
        doc.Dispose();

        if (ActiveDocument == doc)
        {
            ActiveDocument = OpenDocuments.Count == 0
                ? null
                : OpenDocuments[Math.Min(idx, OpenDocuments.Count - 1)];
        }
    }

    [RelayCommand]
    private void CloseOtherDocuments(DocumentViewModelBase? doc)
    {
        if (doc is null) return;
        CloseDocuments(OpenDocuments.Where(d => d != doc).ToList(), activate: doc);
    }

    [RelayCommand]
    private void CloseDocumentsToTheRight(DocumentViewModelBase? doc)
    {
        if (doc is null) return;
        var idx = OpenDocuments.IndexOf(doc);
        if (idx < 0) return;
        CloseDocuments(OpenDocuments.Skip(idx + 1).ToList(), activate: doc);
    }

    [RelayCommand]
    private void CloseSavedDocuments() =>
        CloseDocuments(OpenDocuments.Where(d => !d.IsDirty).ToList(), activate: ActiveDocument);

    [RelayCommand]
    private void CloseAllDocuments() => CloseDocuments(OpenDocuments.ToList(), activate: null);

    /// <summary>
    /// Removes and disposes <paramref name="docs"/>, then re-points
    /// <see cref="ActiveDocument"/>: prefer <paramref name="activate"/> when it
    /// survived the close, otherwise fall back to the last remaining tab (or
    /// null when none are left). Shared by the tab context-menu close actions.
    /// </summary>
    private void CloseDocuments(IReadOnlyList<DocumentViewModelBase> docs, DocumentViewModelBase? activate)
    {
        foreach (var doc in docs)
        {
            OpenDocuments.Remove(doc);
            doc.Dispose();
        }

        if (activate is not null && OpenDocuments.Contains(activate))
            ActiveDocument = activate;
        else if (ActiveDocument is null || !OpenDocuments.Contains(ActiveDocument))
            ActiveDocument = OpenDocuments.LastOrDefault();
    }

    /// <summary>
    /// Saves every dirty document that supports saving, reusing each editor's
    /// own Save flow (including the save-despite-validation-errors prompt — a
    /// cancelled prompt leaves that document dirty and the batch moves on).
    /// </summary>
    [RelayCommand]
    private async Task SaveAll()
    {
        var targets = OpenDocuments
            .Where(d => d.IsDirty && d.FileSaveCommand is not null)
            .ToList();
        if (targets.Count == 0) return;

        var saved = 0;
        foreach (var doc in targets)
        {
            if (doc.FileSaveCommand is IAsyncRelayCommand asyncSave)
                await asyncSave.ExecuteAsync(null);
            else
                doc.FileSaveCommand!.Execute(null);
            if (!doc.IsDirty) saved++;
        }
        StatusText = L("Status_SavedAll", saved, targets.Count);
    }

    /// <summary>
    /// Drops every open document tab and disposes the view-models so their
    /// <see cref="LocalizationService.CultureChanged"/> subscriptions release.
    /// Used by <see cref="OpenSolution"/> when switching solutions.
    /// </summary>
    private void DisposeAndClearOpenDocuments()
    {
        foreach (var doc in OpenDocuments) doc.Dispose();
        OpenDocuments.Clear();
    }

    /// <summary>
    /// Opens a DefinePath folder as the solution and rebuilds the tree. Any open
    /// document tabs from a previous solution are dropped.
    /// </summary>
    public void OpenSolution(string definePath)
    {
        try
        {
            // Materialise any missing framework-default define files into the
            // opened folder before scanning. SkipExisting=true (the default)
            // guarantees consumer customisations are never overwritten — only
            // files the consumer hasn't created yet get written.
            var materialiseResult = Defaults.MaterializeTo(definePath, MaterializeOptions.Default);

            var root = DefinePathScanner.Scan(definePath);
            Nodes = new ObservableCollection<DefineNode> { root };
            Solution = SolutionContext.FromTree(root);
            DisposeAndClearOpenDocuments();
            ActiveDocument = null;
            SelectedNode = null;
            SolutionPath = definePath;

            var loadedMsg = L("Status_SolutionLoaded", Solution.AvailableProgIds.Count);
            StatusText = materialiseResult.WrittenCount > 0
                ? $"{L("Status_FrameworkDefaultsMaterialised", materialiseResult.WrittenCount)} · {loadedMsg}"
                : loadedMsg;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            Nodes = new ObservableCollection<DefineNode>();
            Solution = SolutionContext.Empty;
            DisposeAndClearOpenDocuments();
            ActiveDocument = null;
            SelectedNode = null;
            SolutionPath = string.Empty;
            StatusText = L("Status_OpenSolutionFailed", ex.Message);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822",
        Justification = "RelayCommand handler bound from XAML must remain instance.")]
    [RelayCommand]
    private void ToggleTheme() => App.ToggleTheme();
}
