using System.Collections.ObjectModel;
using Bee.Definition;

namespace Bee.DefineEditor.Models;

/// <summary>
/// The role a node plays in the solution tree.
/// </summary>
public enum DefineNodeKind
{
    /// <summary>The DefinePath root (the "solution").</summary>
    Root,
    /// <summary>A grouping node (by define type, category, or language).</summary>
    Group,
    /// <summary>A concrete define file that can be opened for editing.</summary>
    DefineFile
}

/// <summary>
/// A node in the solution tree that mirrors the DefinePath directory layout.
/// </summary>
public sealed class DefineNode
{
    public required string Name { get; init; }

    public required DefineNodeKind Kind { get; init; }

    /// <summary>The define type this node represents (null for the root).</summary>
    public DefineType? DefineType { get; init; }

    /// <summary>The physical file path; set only on <see cref="DefineNodeKind.DefineFile"/> nodes.</summary>
    public string? FilePath { get; init; }

    /// <summary>A human-readable primary-key summary (e.g. progId or categoryId/tableName).</summary>
    public string? KeyText { get; init; }

    /// <summary>
    /// Resource key of the geometry shown left of the node name in the solution
    /// tree. Looked up in <c>App.axaml</c> via <see cref="Converters.IconKeyToGeometryConverter"/>.
    /// </summary>
    public string Icon { get; init; } = "DefUnknown";

    public bool IsExpanded { get; set; } = true;

    public ObservableCollection<DefineNode> Children { get; } = new();
}
