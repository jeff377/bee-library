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
    private string _statusText = "尚未開啟方案 — 點上方資料夾圖示選擇 DefinePath 目錄";

    [ObservableProperty]
    private DefineNode? _selectedNode;

    /// <summary>
    /// Solution-wide context (currently: the set of available FormSchema ProgIds)
    /// rebuilt each time a DefinePath is opened.
    /// </summary>
    public SolutionContext Solution { get; private set; } = SolutionContext.Empty;

    public bool HasOpenDocuments => OpenDocuments.Count > 0;

    public MainWindowViewModel()
    {
        OpenDocuments.CollectionChanged += OnOpenDocumentsChanged;
    }

    private void OnOpenDocumentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasOpenDocuments));
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
            StatusText = $"已開啟方案：{definePath}（{Solution.AvailableProgIds.Count} 個 FormSchema）";
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            Nodes = new ObservableCollection<DefineNode>();
            Solution = SolutionContext.Empty;
            OpenDocuments.Clear();
            ActiveDocument = null;
            SelectedNode = null;
            StatusText = $"開啟失敗：{ex.Message}";
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822",
        Justification = "RelayCommand handler bound from XAML must remain instance.")]
    [RelayCommand]
    private void ToggleTheme() => App.ToggleTheme();
}
