using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bee.DefineEditor.Models;

/// <summary>
/// Generic tree node used by the singleton settings editors (PermissionModels,
/// DbCategorySettings, ProgramSettings, SystemSettings). Each editor uses string
/// constants for <see cref="Kind"/> (e.g. "Category", "TableItem"); the
/// <see cref="Payload"/> is the underlying mutable Bee.Definition object the
/// property panel binds to, and <see cref="Refresher"/> is the delegate that
/// re-derives header/detail from the payload after edits.
/// </summary>
public sealed partial class SettingsTreeNode : ObservableObject
{
    public required string Icon { get; init; }

    public required string Kind { get; init; }

    public object? Payload { get; init; }

    public SettingsTreeNode? Parent { get; set; }

    public ObservableCollection<SettingsTreeNode> Children { get; } = new();

    [ObservableProperty] private string _header = string.Empty;

    [ObservableProperty] private string? _detail;

    [ObservableProperty] private bool _isExpanded;

    /// <summary>
    /// Delegate invoked by <see cref="RefreshDisplay"/> to rebuild
    /// <see cref="Header"/> / <see cref="Detail"/> from the current
    /// <see cref="Payload"/>. The owning editor supplies it at build time so
    /// each node knows its own formatting without a giant switch.
    /// </summary>
    public Action<SettingsTreeNode>? Refresher { get; init; }

    public void RefreshDisplay() => Refresher?.Invoke(this);

    public void RefreshRecursive()
    {
        RefreshDisplay();
        foreach (var child in Children)
            child.RefreshRecursive();
    }

    public void AddChild(SettingsTreeNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void RemoveSelf()
    {
        Parent?.Children.Remove(this);
        Parent = null;
    }
}
