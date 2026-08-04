using System.Xml.Linq;
using Bee.Definition.Settings;

namespace Bee.Cli;

/// <summary>
/// Splits a pre-flattening <c>ProgramSettings.xml</c> into the flat type registry plus a
/// <c>MenuSettings.xml</c> carrying the grouping that used to live in it.
/// </summary>
/// <remarks>
/// Reads the old layout with LINQ to XML rather than a deserializer: the nested types are gone,
/// and reviving them purely to migrate away from them would be a strange thing to keep in the
/// framework. The output is written through the current types, so it is by construction whatever
/// the runtime expects to read.
/// </remarks>
internal static class SplitMenuMigration
{
    /// <summary>The outcome of a successful split.</summary>
    /// <param name="Registry">The flat type registry.</param>
    /// <param name="Menu">The extracted menu tree.</param>
    internal sealed record Result(ProgramSettings Registry, MenuSettings Menu);

    /// <summary>
    /// Splits the supplied legacy XML.
    /// </summary>
    /// <param name="legacyXml">The content of the old <c>ProgramSettings.xml</c>.</param>
    /// <exception cref="UsageException">
    /// Thrown when the same progId appears under more than one category. The old layout allowed
    /// that and let document order decide the winner; picking one here would cast that accident in
    /// stone, so the ambiguity goes back to the maintainer.
    /// </exception>
    public static Result Split(string legacyXml)
    {
        var doc = XDocument.Parse(legacyXml);
        var root = doc.Root ?? throw new UsageException("ProgramSettings.xml has no root element.");

        var registry = new ProgramSettings();
        var menu = new MenuSettings();
        var duplicates = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int categoryOrder = 0;

        foreach (var category in root.Elements("Categories").Elements("ProgramCategory"))
        {
            categoryOrder += 10;
            string categoryId = Attr(category, "Id");
            var folder = menu.Items!.AddFolder(
                UniqueId(seenIds, string.IsNullOrEmpty(categoryId) ? "folder" : categoryId),
                Attr(category, "DisplayName"));
            folder.Order = categoryOrder;

            int entryOrder = 0;
            foreach (var program in category.Elements("Items").Elements("ProgramItem"))
            {
                entryOrder += 10;
                string progId = Attr(program, "ProgId");
                string displayName = Attr(program, "DisplayName");

                if (registry.Items!.Contains(progId))
                {
                    duplicates.Add($"  {progId} (also under category '{categoryId}')");
                }
                else
                {
                    var item = registry.Items!.Add(progId, displayName);
                    item.BusinessObject = Attr(program, "BusinessObject");
                }

                // The menu entry is kept even for a duplicate progId: appearing in two places is
                // legitimate for a menu, and it is only the registry that must hold one entry.
                var entry = folder.Items!.AddEntry(
                    UniqueId(seenIds, string.IsNullOrEmpty(progId) ? "entry" : progId),
                    progId,
                    displayName);
                entry.Order = entryOrder;
            }
        }

        if (duplicates.Count > 0)
        {
            throw new UsageException(
                "the same progId is registered under more than one category, so the registry entry to keep is ambiguous:"
                + Environment.NewLine + string.Join(Environment.NewLine, duplicates)
                + Environment.NewLine + "Resolve the duplicates in ProgramSettings.xml first, then run this command again.");
        }

        return new Result(registry, menu);
    }

    private static string Attr(XElement element, string name)
        => element.Attribute(name)?.Value ?? string.Empty;

    /// <summary>
    /// Returns <paramref name="candidate"/>, or the first free <c>-2</c>, <c>-3</c>… variant, and
    /// records the result. Folders and entries share one key space, so both go through here.
    /// </summary>
    private static string UniqueId(HashSet<string> seen, string candidate)
    {
        if (seen.Add(candidate)) { return candidate; }
        for (int i = 2; ; i++)
        {
            string next = $"{candidate}-{i}";
            if (seen.Add(next)) { return next; }
        }
    }
}
