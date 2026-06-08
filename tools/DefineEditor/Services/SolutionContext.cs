using Bee.Definition;
using Bee.DefineEditor.Models;

namespace Bee.DefineEditor.Services;

/// <summary>
/// A snapshot of solution-wide information that individual document editors
/// need but cannot derive from a single file (currently: the set of FormSchema
/// ProgIds in the same DefinePath, used to drive RelationProgId / LookupProgId
/// dropdowns and "unknown ProgId" validation).
/// </summary>
public sealed record SolutionContext(IReadOnlyList<string> AvailableProgIds)
{
    public static SolutionContext Empty { get; } = new(Array.Empty<string>());

    /// <summary>
    /// Walks the solution tree and collects all FormSchema ProgIds present.
    /// </summary>
    public static SolutionContext FromTree(DefineNode? root)
    {
        if (root is null) return Empty;
        var progIds = new List<string>();
        Walk(root, progIds);
        progIds.Sort(StringComparer.OrdinalIgnoreCase);
        return new SolutionContext(progIds);
    }

    private static void Walk(DefineNode node, List<string> sink)
    {
        if (node.Kind == DefineNodeKind.DefineFile
            && node.DefineType == DefineType.FormSchema
            && !string.IsNullOrEmpty(node.KeyText))
        {
            sink.Add(node.KeyText);
        }
        foreach (var child in node.Children)
            Walk(child, sink);
    }
}
