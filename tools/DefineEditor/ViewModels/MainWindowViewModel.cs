using System;
using System.Collections.ObjectModel;
using System.IO;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<DefineNode> _nodes = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDocument))]
    private DocumentViewModelBase? _currentDocument;

    [ObservableProperty]
    private string _statusText = "尚未開啟方案 — 點上方資料夾圖示選擇 DefinePath 目錄";

    [ObservableProperty]
    private DefineNode? _selectedNode;

    /// <summary>
    /// Solution-wide context (currently: the set of available FormSchema ProgIds)
    /// rebuilt each time a DefinePath is opened. Flows into each document VM so
    /// that RelationProgId dropdowns and "unknown ProgId" validation can see the
    /// other files in the same solution.
    /// </summary>
    public SolutionContext Solution { get; private set; } = SolutionContext.Empty;

    public bool HasDocument => CurrentDocument is not null;

    partial void OnSelectedNodeChanged(DefineNode? value)
    {
        CurrentDocument = DocumentViewModelFactory.Create(value, Solution);
    }

    /// <summary>
    /// Opens a DefinePath folder as the solution and rebuilds the tree.
    /// </summary>
    public void OpenSolution(string definePath)
    {
        try
        {
            var root = DefinePathScanner.Scan(definePath);
            Nodes = new ObservableCollection<DefineNode> { root };
            Solution = SolutionContext.FromTree(root);
            SelectedNode = null;
            CurrentDocument = null;
            StatusText = $"已開啟方案：{definePath}（{Solution.AvailableProgIds.Count} 個 FormSchema）";
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            Nodes = new ObservableCollection<DefineNode>();
            Solution = SolutionContext.Empty;
            SelectedNode = null;
            CurrentDocument = null;
            StatusText = $"開啟失敗：{ex.Message}";
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822",
        Justification = "RelayCommand handler bound from XAML must remain instance.")]
    [RelayCommand]
    private void ToggleTheme() => App.ToggleTheme();
}
