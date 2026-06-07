using System;
using System.Collections.ObjectModel;
using System.IO;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bee.DefineEditor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<DefineNode> _nodes = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectionSummary))]
    private DefineNode? _selectedNode;

    [ObservableProperty]
    private string _statusText = "尚未開啟方案 — 請開啟 DefinePath 資料夾";

    public string SelectionSummary
    {
        get
        {
            var node = SelectedNode;
            if (node is null)
                return "（未選取）";

            if (node.Kind != DefineNodeKind.DefineFile)
                return $"分組：{node.Name}";

            return string.Join(Environment.NewLine,
                $"型別：{node.DefineType}",
                $"主鍵：{node.KeyText ?? "（單例）"}",
                $"路徑：{node.FilePath}");
        }
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
            SelectedNode = null;
            StatusText = $"已開啟方案：{definePath}";
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            Nodes = new ObservableCollection<DefineNode>();
            StatusText = $"開啟失敗：{ex.Message}";
        }
    }
}
