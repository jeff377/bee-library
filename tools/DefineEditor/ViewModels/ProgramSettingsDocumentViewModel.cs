using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="ProgramSettings"/>. Two-level tree:
/// ProgramSettings → ProgramCategory[] → ProgramItem[]. Validation: duplicate /
/// empty category Ids, duplicate / empty ProgIds within a category.
/// </summary>
public sealed partial class ProgramSettingsDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "ProgramSettings";
    public const string KindCategory = "ProgramCategory";
    public const string KindProgram = "ProgramItem";

    public ProgramSettings Root { get; }

    protected override object RootObject => Root;

    public override string TabIcon => "DefProgramSettings";

    public bool SelectedKindIsRoot => SelectedTreeNode?.Kind == KindRoot;
    public bool SelectedKindIsCategory => SelectedTreeNode?.Kind == KindCategory;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsRoot));
        OnPropertyChanged(nameof(SelectedKindIsCategory));
    }

    private ProgramSettingsDocumentViewModel(string filePath, ProgramSettings root)
        : base(filePath, "ProgramSettings", keyText: string.Empty)
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static ProgramSettingsDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("ProgramSettings file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<ProgramSettings>(filePath)
            ?? throw new InvalidOperationException($"ProgramSettings deserialized to null: {filePath}");
        return new ProgramSettingsDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(ProgramSettings root)
    {
        var node = SettingsTreeNode.Create("DefProgramSettings", KindRoot, root, RefreshRoot, isExpanded: true);
        if (root.Categories is { } categories)
            foreach (var category in categories)
                node.AddChild(BuildCategoryNode(category));
        return node;
    }

    private static SettingsTreeNode BuildCategoryNode(ProgramCategory category)
    {
        var node = SettingsTreeNode.Create("DefCategory", KindCategory, category, RefreshCategory, isExpanded: false);
        if (category.Items is { } items)
            foreach (var program in items)
                node.AddChild(BuildProgramNode(program));
        return node;
    }

    private static SettingsTreeNode BuildProgramNode(ProgramItem program) =>
        SettingsTreeNode.Create("IconBox", KindProgram, program, RefreshProgram, isExpanded: false);

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var root = (ProgramSettings)node.Payload!;
        node.Header = "ProgramSettings";
        node.Detail = $"{root.Categories?.Count ?? 0} ProgramCategory item(s)";
    }

    private static void RefreshCategory(SettingsTreeNode node)
    {
        var c = (ProgramCategory)node.Payload!;
        node.Header = $"{c.Id}  —  {c.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"Id：{c.Id}",
            $"DisplayName：{c.DisplayName}",
            $"Items：{c.Items?.Count ?? 0}");
    }

    private static void RefreshProgram(SettingsTreeNode node)
    {
        var p = (ProgramItem)node.Payload!;
        node.Header = $"{p.ProgId}  —  {p.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"ProgId：{p.ProgId}",
            $"DisplayName：{p.DisplayName}",
            $"BusinessObject：{p.BusinessObject}");
    }

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
    private void AddCategory()
    {
        if (SelectedTreeNode is not { Kind: KindRoot, Payload: ProgramSettings root } rootNode)
            return;
        var id = UniqueKey(root.Categories!.Select(c => c.Id), "new_category");
        var category = new ProgramCategory(id, "New category");
        root.Categories!.Add(category);
        var node = BuildCategoryNode(category);
        rootNode.AddChild(node);
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "ProgramCategory", id);
    }

    private bool CanAddCategory() => SelectedTreeNode?.Kind == KindRoot;

    [RelayCommand(CanExecute = nameof(CanAddProgram))]
    private void AddProgram()
    {
        var categoryNode = FindAncestor(SelectedTreeNode, KindCategory);
        if (categoryNode?.Payload is not ProgramCategory category) return;
        var id = UniqueKey(category.Items!.Select(p => p.ProgId), "NewProgram");
        var program = new ProgramItem { ProgId = id, DisplayName = "New program" };
        category.Items!.Add(program);
        var node = BuildProgramNode(program);
        categoryNode.AddChild(node);
        categoryNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "ProgramItem", id);
    }

    private bool CanAddProgram() => FindAncestor(SelectedTreeNode, KindCategory) is not null;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindCategory when node.Payload is ProgramCategory c && node.Parent?.Payload is ProgramSettings p
            => () => p.Categories!.Remove(c),
        KindProgram when node.Payload is ProgramItem prog && node.Parent?.Payload is ProgramCategory pc
            => () => pc.Items!.Remove(prog),
        _ => null,
    };

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
    {
        var issues = new List<ValidationIssue>();
        var categories = Root.Categories;
        if (categories is null || categories.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, "ProgramSettings", "No ProgramCategory has been defined."));
            return issues;
        }
        var seenCategoryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in categories)
        {
            var catPath = !string.IsNullOrWhiteSpace(category.Id) ? category.Id : "(unnamed)";
            if (string.IsNullOrWhiteSpace(category.Id))
                issues.Add(new(ValidationSeverity.Error, catPath, "ProgramCategory.Id cannot be empty."));
            else if (!seenCategoryIds.Add(category.Id))
                issues.Add(new(ValidationSeverity.Error, catPath,
                    $"ProgramCategory.Id '{category.Id}' is a duplicate."));

            var seenProgIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var program in category.Items ?? Enumerable.Empty<ProgramItem>())
            {
                var path = $"{catPath}.{(string.IsNullOrEmpty(program.ProgId) ? "(unnamed)" : program.ProgId)}";
                if (string.IsNullOrWhiteSpace(program.ProgId))
                    issues.Add(new(ValidationSeverity.Error, path, "ProgramItem.ProgId cannot be empty."));
                else if (!seenProgIds.Add(program.ProgId))
                    issues.Add(new(ValidationSeverity.Error, path,
                        $"ProgramItem.ProgId '{program.ProgId}' is a duplicate within '{catPath}'."));
            }
        }
        return issues;
    }
}
