using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Services;
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

    private void OnOpenDocumentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasOpenDocuments));
        OnPropertyChanged(nameof(ShowDocumentWelcome));
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

        if (ActiveDocument == doc)
        {
            ActiveDocument = OpenDocuments.Count == 0
                ? null
                : OpenDocuments[Math.Min(idx, OpenDocuments.Count - 1)];
        }
    }

    /// <summary>
    /// Opens a DefinePath folder as the solution and rebuilds the tree. Any open
    /// document tabs from a previous solution are dropped.
    /// </summary>
    public void OpenSolution(string definePath)
    {
        try
        {
            var root = DefinePathScanner.Scan(definePath);
            Nodes = new ObservableCollection<DefineNode> { root };
            Solution = SolutionContext.FromTree(root);
            OpenDocuments.Clear();
            ActiveDocument = null;
            SelectedNode = null;
            SolutionPath = definePath;
            StatusText = L("Status_SolutionLoaded", Solution.AvailableProgIds.Count);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            Nodes = new ObservableCollection<DefineNode>();
            Solution = SolutionContext.Empty;
            OpenDocuments.Clear();
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
