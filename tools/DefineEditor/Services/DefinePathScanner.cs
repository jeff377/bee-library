using System;
using System.IO;
using System.Linq;
using Bee.Definition;
using Bee.DefineEditor.Models;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Scans a DefinePath directory tree and builds the solution node hierarchy.
/// The framework exposes no enumeration API, so the tool discovers define files
/// by scanning the directory using the known file-name conventions.
/// </summary>
public static class DefinePathScanner
{
    private sealed record SingletonSpec(string FileName, DefineType Type, string Display, string Icon);

    private static readonly SingletonSpec[] s_singletons =
    {
        new("SystemSettings.xml", DefineType.SystemSettings, "SystemSettings", "DefSystemSettings"),
        new("DatabaseSettings.xml", DefineType.DatabaseSettings, "DatabaseSettings", "DefDatabaseSettings"),
        new("DbCategorySettings.xml", DefineType.DbCategorySettings, "DbCategorySettings", "DefDbCategorySettings"),
        new("ProgramSettings.xml", DefineType.ProgramSettings, "ProgramSettings", "DefProgramSettings"),
        new("PermissionModels.xml", DefineType.PermissionModels, "PermissionModels", "DefPermissionModels"),
    };

    private const string IconRoot = "DefRoot";
    private const string IconSingletonGroup = "DefSystemGroup";
    private const string IconCategory = "DefCategory";
    private const string IconFormSchema = "DefFormSchema";
    private const string IconTableSchema = "DefTableSchema";
    private const string IconFormLayout = "DefFormLayout";
    private const string IconLanguage = "DefLanguage";

    /// <summary>
    /// Scans <paramref name="definePath"/> and returns the root solution node.
    /// </summary>
    public static DefineNode Scan(string definePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definePath);
        if (!Directory.Exists(definePath))
            throw new DirectoryNotFoundException($"DefinePath not found: {definePath}");

        var root = new DefineNode
        {
            Name = Path.GetFileName(definePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            Kind = DefineNodeKind.Root,
            FilePath = definePath,
            Icon = IconRoot,
        };

        AddSingletonGroup(root, definePath);
        AddFlatGroup(root, definePath, "FormSchema", "*.FormSchema.xml", ".FormSchema.xml", DefineType.FormSchema, IconFormSchema);
        AddTwoLevelGroup(root, definePath, "TableSchema", "*.TableSchema.xml", ".TableSchema.xml", DefineType.TableSchema, IconTableSchema);
        AddFlatGroup(root, definePath, "FormLayout", "*.FormLayout.xml", ".FormLayout.xml", DefineType.FormLayout, IconFormLayout);
        AddTwoLevelGroup(root, definePath, "Language", "*.Language.xml", ".Language.xml", DefineType.Language, IconLanguage);

        return root;
    }

    private static void AddSingletonGroup(DefineNode root, string definePath)
    {
        var group = new DefineNode
        {
            Name = "System",
            Kind = DefineNodeKind.Group,
            Icon = IconSingletonGroup,
        };
        foreach (var spec in s_singletons)
        {
            var path = Path.Combine(definePath, spec.FileName);
            if (File.Exists(path))
            {
                group.Children.Add(new DefineNode
                {
                    Name = spec.Display,
                    Kind = DefineNodeKind.DefineFile,
                    DefineType = spec.Type,
                    FilePath = path,
                    Icon = spec.Icon,
                });
            }
        }
        if (group.Children.Count > 0)
            root.Children.Add(group);
    }

    private static void AddFlatGroup(
        DefineNode root, string definePath, string subDir, string pattern, string suffix, DefineType type, string icon)
    {
        var dir = Path.Combine(definePath, subDir);
        if (!Directory.Exists(dir))
            return;

        var group = new DefineNode
        {
            Name = subDir,
            Kind = DefineNodeKind.Group,
            DefineType = type,
            Icon = icon,
        };
        foreach (var file in EnumerateOrdered(dir, pattern))
        {
            var key = TrimSuffix(Path.GetFileName(file), suffix);
            group.Children.Add(new DefineNode
            {
                Name = key,
                Kind = DefineNodeKind.DefineFile,
                DefineType = type,
                FilePath = file,
                KeyText = key,
                Icon = icon,
            });
        }
        if (group.Children.Count > 0)
            root.Children.Add(group);
    }

    private static void AddTwoLevelGroup(
        DefineNode root, string definePath, string subDir, string pattern, string suffix, DefineType type, string icon)
    {
        var dir = Path.Combine(definePath, subDir);
        if (!Directory.Exists(dir))
            return;

        var group = new DefineNode
        {
            Name = subDir,
            Kind = DefineNodeKind.Group,
            DefineType = type,
            Icon = icon,
        };
        foreach (var categoryDir in Directory.EnumerateDirectories(dir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var category = Path.GetFileName(categoryDir);
            var categoryNode = new DefineNode
            {
                Name = category,
                Kind = DefineNodeKind.Group,
                DefineType = type,
                Icon = IconCategory,
            };
            foreach (var file in EnumerateOrdered(categoryDir, pattern))
            {
                var leaf = TrimSuffix(Path.GetFileName(file), suffix);
                categoryNode.Children.Add(new DefineNode
                {
                    Name = leaf,
                    Kind = DefineNodeKind.DefineFile,
                    DefineType = type,
                    FilePath = file,
                    KeyText = $"{category}/{leaf}",
                    Icon = icon,
                });
            }
            if (categoryNode.Children.Count > 0)
                group.Children.Add(categoryNode);
        }
        if (group.Children.Count > 0)
            root.Children.Add(group);
    }

    private static System.Collections.Generic.IEnumerable<string> EnumerateOrdered(string dir, string pattern)
        => Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

    private static string TrimSuffix(string fileName, string suffix)
        => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? fileName[..^suffix.Length]
            : fileName;
}
