using Bee.Base.Serialization;
using Bee.Definition.Language;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="LanguageResource"/>. Tree: LanguageResource → Items
/// group → LanguageItem[]; LanguageResource → Enums group → LanguageEnum[] →
/// LanguageEnumEntry[]. Single resource = single namespace × single language.
/// </summary>
public sealed partial class LanguageDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "LanguageResource";
    public const string KindItemsGroup = "ItemsGroup";
    public const string KindItem = "LanguageItem";
    public const string KindEnumsGroup = "EnumsGroup";
    public const string KindEnum = "LanguageEnum";
    public const string KindEnumEntry = "LanguageEnumEntry";

    public LanguageResource Root { get; }
    protected override object RootObject => Root;

    public override string TabIcon => "DefLanguage";

    public bool SelectedKindIsItemsGroup => SelectedTreeNode?.Kind == KindItemsGroup;
    public bool SelectedKindIsEnumsGroup => SelectedTreeNode?.Kind == KindEnumsGroup;
    public bool SelectedKindIsEnum => SelectedTreeNode?.Kind == KindEnum;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsItemsGroup));
        OnPropertyChanged(nameof(SelectedKindIsEnumsGroup));
        OnPropertyChanged(nameof(SelectedKindIsEnum));
    }

    private LanguageDocumentViewModel(string filePath, LanguageResource root)
        : base(filePath, "Language", keyText: string.IsNullOrEmpty(root.Lang) ? root.Namespace : $"{root.Lang}/{root.Namespace}")
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static LanguageDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Language file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<LanguageResource>(filePath)
            ?? throw new InvalidOperationException($"LanguageResource deserialized to null: {filePath}");
        return new LanguageDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(LanguageResource root)
    {
        var rootNode = SettingsTreeNode.Create("DefLanguage", KindRoot, root, RefreshRoot, isExpanded: true);

        var itemsGroup = SettingsTreeNode.Create("IconText", KindItemsGroup, root, RefreshItemsGroup, isExpanded: true);
        foreach (var item in root.Items)
            itemsGroup.AddChild(BuildItemNode(item));
        rootNode.AddChild(itemsGroup);

        var enumsGroup = SettingsTreeNode.Create("IconList", KindEnumsGroup, root, RefreshEnumsGroup, isExpanded: true);
        foreach (var enumDef in root.Enums)
            enumsGroup.AddChild(BuildEnumNode(enumDef));
        rootNode.AddChild(enumsGroup);

        return rootNode;
    }

    private static SettingsTreeNode BuildItemNode(LanguageItem item) =>
        SettingsTreeNode.Create("IconDot", KindItem, item, RefreshItem, isExpanded: false);

    private static SettingsTreeNode BuildEnumNode(LanguageEnum enumDef)
    {
        var node = SettingsTreeNode.Create("IconList", KindEnum, enumDef, RefreshEnum, isExpanded: false);
        foreach (var entry in enumDef.Entries)
            node.AddChild(BuildEnumEntryNode(entry));
        return node;
    }

    private static SettingsTreeNode BuildEnumEntryNode(LanguageEnumEntry entry) =>
        SettingsTreeNode.Create("IconDot", KindEnumEntry, entry, RefreshEnumEntry, isExpanded: false);

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var r = (LanguageResource)node.Payload!;
        node.Header = $"{r.Namespace} [{r.Lang}]";
        node.Detail = string.Join(Environment.NewLine,
            $"Namespace：{r.Namespace}",
            $"Lang：{r.Lang}",
            $"Items：{r.Items.Count}，Enums：{r.Enums.Count}");
    }

    private static void RefreshItemsGroup(SettingsTreeNode node)
    {
        var r = (LanguageResource)node.Payload!;
        node.Header = $"Items ({r.Items.Count})";
        node.Detail = "Localized key/value text entry.";
    }

    private static void RefreshEnumsGroup(SettingsTreeNode node)
    {
        var r = (LanguageResource)node.Payload!;
        node.Header = $"Enums ({r.Enums.Count})";
        node.Detail = "code/text set (used for dropdowns / lookups).";
    }

    private static void RefreshItem(SettingsTreeNode node)
    {
        var i = (LanguageItem)node.Payload!;
        node.Header = $"{i.Key}  =  {i.Value}";
        node.Detail = $"Key：{i.Key}\nValue：{i.Value}";
    }

    private static void RefreshEnum(SettingsTreeNode node)
    {
        var e = (LanguageEnum)node.Payload!;
        node.Header = $"{e.Name}  ({e.Entries.Count} entries)";
        node.Detail = $"Name：{e.Name}\nEntries：{e.Entries.Count}";
    }

    private static void RefreshEnumEntry(SettingsTreeNode node)
    {
        var e = (LanguageEnumEntry)node.Payload!;
        node.Header = $"{e.Code}  =  {e.Text}";
        node.Detail = $"Code：{e.Code}\nText：{e.Text}";
    }

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    private void AddItem()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindItemsGroup)
                        ?? Roots[0].Children.FirstOrDefault(c => c.Kind == KindItemsGroup);
        if (groupNode is null) return;
        var key = UniqueKey(Root.Items.Select(i => i.Key), "NewKey");
        var item = new LanguageItem { Key = key, Value = "New text" };
        Root.Items.Add(item);
        var node = BuildItemNode(item);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "LanguageItem", key);
    }
    private bool CanAddItem() => SelectedTreeNode is not null;

    [RelayCommand(CanExecute = nameof(CanAddEnum))]
    private void AddEnum()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindEnumsGroup)
                        ?? Roots[0].Children.FirstOrDefault(c => c.Kind == KindEnumsGroup);
        if (groupNode is null) return;
        var name = UniqueKey(Root.Enums.Select(e => e.Name), "NewEnum");
        var enumDef = new LanguageEnum { Name = name };
        Root.Enums.Add(enumDef);
        var node = BuildEnumNode(enumDef);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "LanguageEnum", name);
    }
    private bool CanAddEnum() => SelectedTreeNode is not null;

    [RelayCommand(CanExecute = nameof(CanAddEntry))]
    private void AddEntry()
    {
        var enumNode = FindAncestor(SelectedTreeNode, KindEnum);
        if (enumNode?.Payload is not LanguageEnum enumDef) return;
        var code = UniqueKey(enumDef.Entries.Select(e => e.Code), "code");
        var entry = new LanguageEnumEntry { Code = code, Text = "New entry" };
        enumDef.Entries.Add(entry);
        var node = BuildEnumEntryNode(entry);
        enumNode.AddChild(node);
        enumNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "LanguageEnumEntry", code);
    }
    private bool CanAddEntry() => FindAncestor(SelectedTreeNode, KindEnum) is not null;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindItem when node.Payload is LanguageItem li => () => Root.Items.Remove(li),
        KindEnum when node.Payload is LanguageEnum le => () => Root.Enums.Remove(le),
        KindEnumEntry when node.Payload is LanguageEnumEntry lee
            && node.Parent?.Payload is LanguageEnum parentEnum
            => () => parentEnum.Entries.Remove(lee),
        _ => null,
    };

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
    {
        var issues = new List<ValidationIssue>();
        if (string.IsNullOrWhiteSpace(Root.Namespace))
            issues.Add(new(ValidationSeverity.Error, "LanguageResource", "Namespace cannot be empty."));
        if (string.IsNullOrWhiteSpace(Root.Lang))
            issues.Add(new(ValidationSeverity.Error, "LanguageResource", "Lang cannot be empty (recommended: BCP-47 codes such as zh-TW / en-US)."));

        var itemKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Root.Items)
        {
            var path = string.IsNullOrEmpty(item.Key) ? "Items[?]" : $"Items.{item.Key}";
            if (string.IsNullOrWhiteSpace(item.Key))
                issues.Add(new(ValidationSeverity.Error, path, "LanguageItem.Key cannot be empty."));
            else if (!itemKeys.Add(item.Key))
                issues.Add(new(ValidationSeverity.Error, path, $"Item.Key '{item.Key}' is a duplicate."));
        }

        var enumNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var enumDef in Root.Enums)
        {
            var ePath = string.IsNullOrEmpty(enumDef.Name) ? "Enums[?]" : $"Enums.{enumDef.Name}";
            if (string.IsNullOrWhiteSpace(enumDef.Name))
                issues.Add(new(ValidationSeverity.Error, ePath, "LanguageEnum.Name cannot be empty."));
            else if (!enumNames.Add(enumDef.Name))
                issues.Add(new(ValidationSeverity.Error, ePath, $"Enum.Name '{enumDef.Name}' is a duplicate."));

            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in enumDef.Entries)
            {
                var entryPath = $"{ePath}.{(string.IsNullOrEmpty(entry.Code) ? "(unnamed)" : entry.Code)}";
                if (string.IsNullOrWhiteSpace(entry.Code))
                    issues.Add(new(ValidationSeverity.Error, entryPath, "Entry.Code cannot be empty."));
                else if (!codes.Add(entry.Code))
                    issues.Add(new(ValidationSeverity.Error, entryPath,
                        $"Entry.Code '{entry.Code}' is a duplicate within '{enumDef.Name}'."));
            }
        }
        return issues;
    }
}
