using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bee.DefineEditor.Models;

/// <summary>
/// A node in the FormSchema inner structure tree. Holds a reference to its
/// underlying Bee.Definition payload (FormSchema / FormTable / FormField /
/// FieldMapping / ListItem) so the property panel can edit it directly and
/// Add/Delete commands can mutate the owning collection.
/// </summary>
public sealed partial class FormSchemaTreeNode : ObservableObject
{
    public required string Icon { get; init; }

    public required FormSchemaNodeKind Kind { get; init; }

    /// <summary>
    /// The underlying mutable object (<c>FormSchema</c>, <c>FormTable</c>,
    /// <c>FormField</c>, <c>FieldMapping</c>, or <c>ListItem</c>). Group nodes
    /// (Relation / Lookup / ListItems) carry the owning <c>FormField</c> as
    /// payload so they can derive a header from it.
    /// </summary>
    public object? Payload { get; init; }

    public FormSchemaTreeNode? Parent { get; set; }

    public ObservableCollection<FormSchemaTreeNode> Children { get; } = new();

    [ObservableProperty]
    private string _header = string.Empty;

    [ObservableProperty]
    private string? _detail;

    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Rebuilds <see cref="Header"/> and <see cref="Detail"/> from the current
    /// <see cref="Payload"/> state. Call after editing payload properties so
    /// the tree reflects the new values.
    /// </summary>
    public void RefreshDisplay()
    {
        var (header, detail) = FormSchemaNodeDisplay.For(Kind, Payload);
        Header = header;
        Detail = detail;
    }

    public void RefreshRecursive()
    {
        RefreshDisplay();
        foreach (var child in Children)
            child.RefreshRecursive();
    }

    public void AddChild(FormSchemaTreeNode child)
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
