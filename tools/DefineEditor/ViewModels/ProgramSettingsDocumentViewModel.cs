using Bee.Base;
using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="ProgramSettings"/>. Flat tree: ProgramSettings → ProgramItem[].
/// Validation: empty or duplicate ProgIds across the whole registry.
/// </summary>
public sealed partial class ProgramSettingsDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "ProgramSettings";
    public const string KindProgram = "ProgramItem";

    public ProgramSettings Root { get; }

    protected override object RootObject => Root;

    public override string TabIcon => "DefProgramSettings";

    public bool SelectedKindIsRoot => SelectedTreeNode?.Kind == KindRoot;

    protected override bool HasVisibleAddMenuItems => SelectedKindIsRoot;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsRoot));
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
        // The same guard the runtime storages apply: an un-migrated file would otherwise open as an
        // empty registry, and saving it would silently discard every entry it still contains.
        var xml = FileUtilities.FileReadText(filePath);
        ProgramSettingsFormat.EnsureCurrentFormat(xml, filePath);
        var root = XmlCodec.Deserialize<ProgramSettings>(xml)
            ?? throw new InvalidOperationException($"ProgramSettings deserialized to null: {filePath}");
        root.SetObjectFilePath(filePath);
        return new ProgramSettingsDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(ProgramSettings root)
    {
        var node = SettingsTreeNode.Create("DefProgramSettings", KindRoot, root, RefreshRoot, isExpanded: true);
        if (root.Items is { } items)
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
        node.Detail = $"{root.Items?.Count ?? 0} ProgramItem(s)";
    }

    private static void RefreshProgram(SettingsTreeNode node)
    {
        var p = (ProgramItem)node.Payload!;
        node.Header = $"{p.ProgId}  —  {p.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"ProgId：{p.ProgId}",
            $"DisplayName：{p.DisplayName}",
            $"BusinessObject：{p.BusinessObject}",
            $"Repository：{p.Repository}");
    }

    [RelayCommand(CanExecute = nameof(CanAddProgram))]
    private void AddProgram()
    {
        if (SelectedTreeNode is not { Kind: KindRoot, Payload: ProgramSettings root } rootNode)
            return;
        var id = UniqueKey(root.Items!.Select(p => p.ProgId), "NewProgram");
        var program = new ProgramItem { ProgId = id, DisplayName = "New program" };
        root.Items!.Add(program);
        var node = BuildProgramNode(program);
        rootNode.AddChild(node);
        rootNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "ProgramItem", id);
    }

    private bool CanAddProgram() => SelectedTreeNode?.Kind == KindRoot;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindProgram when node.Payload is ProgramItem prog && node.Parent?.Payload is ProgramSettings p
            => () => p.Items!.Remove(prog),
        _ => null,
    };

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
    {
        var issues = new List<ValidationIssue>();
        var items = Root.Items;
        if (items is null || items.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, "ProgramSettings", "No ProgramItem has been registered."));
            return issues;
        }

        // The registry is flat, so duplicate detection is global — which is the point of the flat
        // shape. Under the earlier nested layout this check only ever ran within one category.
        var seenProgIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var program in items)
        {
            var path = string.IsNullOrEmpty(program.ProgId) ? "(unnamed)" : program.ProgId;
            if (string.IsNullOrWhiteSpace(program.ProgId))
                issues.Add(new(ValidationSeverity.Error, path, "ProgramItem.ProgId cannot be empty."));
            else if (!seenProgIds.Add(program.ProgId))
                issues.Add(new(ValidationSeverity.Error, path,
                    $"ProgramItem.ProgId '{program.ProgId}' is a duplicate."));
        }
        return issues;
    }
}
