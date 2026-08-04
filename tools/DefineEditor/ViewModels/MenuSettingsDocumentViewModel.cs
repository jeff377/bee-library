using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="MenuSettings"/>. Arbitrarily deep tree:
/// MenuSettings → (MenuFolder | MenuEntry)*, folders owning children.
/// Validation: empty or duplicate node Ids across the whole tree, empty ProgIds,
/// and — when the sibling ProgramSettings.xml is present — entries pointing at an
/// unregistered progId.
/// </summary>
public sealed partial class MenuSettingsDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "MenuSettings";
    public const string KindFolder = "MenuFolder";
    public const string KindEntry = "MenuEntry";

    public MenuSettings Root { get; }

    protected override object RootObject => Root;

    public override string TabIcon => "DefMenuSettings";

    public bool SelectedKindIsRoot => SelectedTreeNode?.Kind == KindRoot;
    public bool SelectedKindIsFolder => SelectedTreeNode?.Kind == KindFolder;

    /// <summary>
    /// A folder or the root can own children, so both accept Add.
    /// </summary>
    public bool SelectedKindCanOwnChildren => SelectedKindIsRoot || SelectedKindIsFolder;

    protected override bool HasVisibleAddMenuItems => SelectedKindCanOwnChildren;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsRoot));
        OnPropertyChanged(nameof(SelectedKindIsFolder));
        OnPropertyChanged(nameof(SelectedKindCanOwnChildren));
    }

    private MenuSettingsDocumentViewModel(string filePath, MenuSettings root)
        : base(filePath, "MenuSettings", keyText: string.Empty)
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static MenuSettingsDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("MenuSettings file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<MenuSettings>(filePath)
            ?? throw new InvalidOperationException($"MenuSettings deserialized to null: {filePath}");
        // Deliberately not EnsureValid here: an editor must be able to open a broken definition in
        // order to fix it. The same problems are reported through the validation pane instead.
        return new MenuSettingsDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(MenuSettings root)
    {
        var node = SettingsTreeNode.Create("DefMenuSettings", KindRoot, root, RefreshRoot, isExpanded: true);
        AddChildNodes(node, root.Items);
        return node;
    }

    private static void AddChildNodes(SettingsTreeNode parent, MenuNodeCollection? nodes)
    {
        if (nodes is null) { return; }
        foreach (var child in nodes)
            parent.AddChild(BuildNode(child));
    }

    private static SettingsTreeNode BuildNode(MenuNodeBase node)
    {
        if (node is MenuFolder folder)
        {
            var folderNode = SettingsTreeNode.Create("DefCategory", KindFolder, folder, RefreshFolder, isExpanded: true);
            AddChildNodes(folderNode, folder.Items);
            return folderNode;
        }
        return SettingsTreeNode.Create("IconBox", KindEntry, node, RefreshEntry, isExpanded: false);
    }

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var root = (MenuSettings)node.Payload!;
        node.Header = "MenuSettings";
        node.Detail = $"{root.EnumerateNodes().Count()} node(s)";
    }

    private static void RefreshFolder(SettingsTreeNode node)
    {
        var f = (MenuFolder)node.Payload!;
        node.Header = $"{f.Id}  —  {f.Caption}";
        node.Detail = string.Join(Environment.NewLine,
            $"Id：{f.Id}",
            $"Caption：{f.Caption}",
            $"Order：{f.Order}",
            $"Visible：{f.Visible}",
            $"Items：{f.Items?.Count ?? 0}");
    }

    private static void RefreshEntry(SettingsTreeNode node)
    {
        var e = (MenuEntry)node.Payload!;
        node.Header = $"{e.Id}  —  {e.Caption}";
        node.Detail = string.Join(Environment.NewLine,
            $"Id：{e.Id}",
            $"ProgId：{e.ProgId}",
            $"Caption：{e.Caption}",
            $"Order：{e.Order}",
            $"Visible：{e.Visible}");
    }

    /// <summary>
    /// Returns the collection owned by the selected node, or <c>null</c> when the selection cannot
    /// own children.
    /// </summary>
    private MenuNodeCollection? SelectedOwnerCollection(out SettingsTreeNode? ownerNode)
    {
        ownerNode = SelectedTreeNode;
        return ownerNode?.Payload switch
        {
            MenuSettings settings => settings.Items,
            MenuFolder folder => folder.Items,
            _ => null,
        };
    }

    [RelayCommand(CanExecute = nameof(CanAddNode))]
    private void AddFolder() => AddNode(isFolder: true);

    [RelayCommand(CanExecute = nameof(CanAddNode))]
    private void AddEntry() => AddNode(isFolder: false);

    private void AddNode(bool isFolder)
    {
        var owner = SelectedOwnerCollection(out var ownerNode);
        if (owner is null || ownerNode is null) { return; }

        // Ids are unique across the whole tree, not merely among siblings, so the candidate is
        // checked against every existing node.
        var id = UniqueKey(Root.EnumerateNodes().Select(n => n.Id), isFolder ? "new-folder" : "new-entry");
        MenuNodeBase node = isFolder
            ? new MenuFolder(id, "New folder")
            : new MenuEntry(id, string.Empty, "New entry");
        owner.Add(node);

        var treeNode = BuildNode(node);
        ownerNode.AddChild(treeNode);
        ownerNode.IsExpanded = true;
        SelectedTreeNode = treeNode;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", isFolder ? "MenuFolder" : "MenuEntry", id);
    }

    private bool CanAddNode() => SelectedKindCanOwnChildren;

    protected override Action? GetDeleteAction(SettingsTreeNode node)
    {
        if (node.Payload is not MenuNodeBase target) { return null; }
        var owner = node.Parent?.Payload switch
        {
            MenuSettings settings => settings.Items,
            MenuFolder folder => folder.Items,
            _ => null,
        };
        return owner is null ? null : () => owner.Remove(target);
    }

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
    {
        var issues = new List<ValidationIssue>();
        if (Root.Items is null || Root.Items.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, "MenuSettings", "The menu has no nodes."));
            return issues;
        }

        // The registry lives beside the menu in a DefinePath, so referential integrity can be
        // checked here. It is skipped rather than reported when the file is absent or in the old
        // layout: opening the menu editor is not the place to fail over the registry's state.
        ProgramSettings? registry = TryLoadRegistry();

        foreach (var problem in Root.Validate(registry))
            issues.Add(new(ValidationSeverity.Error, "MenuSettings", problem));

        foreach (var folder in Root.EnumerateNodes().OfType<MenuFolder>())
        {
            if (folder.Items is null || folder.Items.Count == 0)
                issues.Add(new(ValidationSeverity.Warning, folder.Id, $"MenuFolder '{folder.Id}' is empty."));
        }

        return issues;
    }

    private ProgramSettings? TryLoadRegistry()
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (string.IsNullOrEmpty(dir)) { return null; }
        var registryPath = Path.Combine(dir, "ProgramSettings.xml");
        if (!File.Exists(registryPath)) { return null; }

        try
        {
            return XmlCodec.DeserializeFromFile<ProgramSettings>(registryPath);
        }
        catch (InvalidOperationException)
        {
            // Unreadable or legacy-layout registry: the ProgramSettings editor reports that, and
            // the menu's own structural findings are still worth showing.
            return null;
        }
    }
}
