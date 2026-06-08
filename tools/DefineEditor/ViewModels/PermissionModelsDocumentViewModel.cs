using Bee.Base.Serialization;
using Bee.Definition.Settings;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="PermissionModels"/>. Two-level tree:
/// PermissionModels → PermissionModel[] → PermissionRule[]. Validation wraps the
/// built-in <see cref="PermissionModels.Validate"/> plus duplicate / empty
/// ModelId checks the framework method doesn't catch.
/// </summary>
public sealed partial class PermissionModelsDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "PermissionModels";
    public const string KindModel = "PermissionModel";
    public const string KindRule = "PermissionRule";

    public PermissionModels Root { get; }

    protected override object RootObject => Root;

    public override string TabIcon => "DefPermissionModels";

    public bool SelectedKindIsRoot => SelectedTreeNode?.Kind == KindRoot;
    public bool SelectedKindIsModel => SelectedTreeNode?.Kind == KindModel;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsRoot));
        OnPropertyChanged(nameof(SelectedKindIsModel));
    }

    private PermissionModelsDocumentViewModel(string filePath, PermissionModels root)
        : base(filePath, "PermissionModels", keyText: string.Empty)
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static PermissionModelsDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("PermissionModels file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<PermissionModels>(filePath)
            ?? throw new InvalidOperationException($"PermissionModels deserialized to null: {filePath}");
        return new PermissionModelsDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(PermissionModels root)
    {
        var node = SettingsTreeNode.Create("DefPermissionModels", KindRoot, root, RefreshRoot, isExpanded: true);
        if (root.Models is { } models)
            foreach (var model in models)
                node.AddChild(BuildModelNode(model));
        return node;
    }

    private static SettingsTreeNode BuildModelNode(PermissionModel model)
    {
        var node = SettingsTreeNode.Create("IconBox", KindModel, model, RefreshModel, isExpanded: false);
        if (model.Rules is { } rules)
            foreach (var rule in rules)
                node.AddChild(BuildRuleNode(rule));
        return node;
    }

    private static SettingsTreeNode BuildRuleNode(PermissionRule rule) =>
        SettingsTreeNode.Create("IconKey", KindRule, rule, RefreshRule, isExpanded: false);

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var root = (PermissionModels)node.Payload!;
        node.Header = "PermissionModels";
        node.Detail = $"{root.Models?.Count ?? 0} PermissionModel item(s)";
    }

    private static void RefreshModel(SettingsTreeNode node)
    {
        var m = (PermissionModel)node.Payload!;
        node.Header = $"{m.ModelId}  —  {m.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"ModelId：{m.ModelId}",
            $"DisplayName：{m.DisplayName}",
            $"Rules：{m.Rules?.Count ?? 0}");
    }

    private static void RefreshRule(SettingsTreeNode node)
    {
        var r = (PermissionRule)node.Payload!;
        node.Header = $"{r.Action}  →  {r.Scope}";
        node.Detail = string.Join(Environment.NewLine,
            $"Action：{r.Action}",
            $"Scope：{r.Scope}");
    }

    [RelayCommand(CanExecute = nameof(CanAddModel))]
    private void AddModel()
    {
        if (SelectedTreeNode is not { Kind: KindRoot, Payload: PermissionModels root } rootNode)
            return;
        var modelId = UniqueKey(root.Models!.Select(m => m.ModelId), "NewModel");
        var model = new PermissionModel(modelId, "New model");
        root.Models!.Add(model);
        var node = BuildModelNode(model);
        rootNode.AddChild(node);
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "PermissionModel", modelId);
    }

    private bool CanAddModel() => SelectedTreeNode?.Kind == KindRoot;

    [RelayCommand(CanExecute = nameof(CanAddRule))]
    private void AddRule()
    {
        var modelNode = FindAncestor(SelectedTreeNode, KindModel);
        if (modelNode?.Payload is not PermissionModel model) return;
        var action = PickAvailableAction(model);
        if (action is null)
        {
            StatusText = L("Status_PermissionRuleCovered");
            return;
        }
        var rule = new PermissionRule(action.Value);
        model.Rules!.Add(rule);
        var node = BuildRuleNode(rule);
        modelNode.AddChild(node);
        modelNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "PermissionRule", action.Value);
    }

    private bool CanAddRule() => FindAncestor(SelectedTreeNode, KindModel) is not null;

    private static PermissionAction? PickAvailableAction(PermissionModel model)
    {
        var taken = new HashSet<PermissionAction>(
            (model.Rules ?? Enumerable.Empty<PermissionRule>()).Select(r => r.Action));
        foreach (var candidate in EditorOptions.PermissionActions)
            if (!taken.Contains(candidate)) return candidate;
        return null;
    }

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindModel when node.Payload is PermissionModel m && node.Parent?.Payload is PermissionModels p
            => () => p.Models!.Remove(m),
        KindRule when node.Payload is PermissionRule r && node.Parent?.Payload is PermissionModel pm
            => () => pm.Rules!.Remove(r),
        _ => null,
    };

    protected override IReadOnlyList<ValidationIssue> PerformValidation()
    {
        var issues = new List<ValidationIssue>();
        var models = Root.Models;
        if (models is null || models.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, "PermissionModels", "No PermissionModel has been defined."));
        }
        else
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var model in models)
            {
                var path = !string.IsNullOrWhiteSpace(model.ModelId) ? model.ModelId : "(unnamed)";
                if (string.IsNullOrWhiteSpace(model.ModelId))
                    issues.Add(new(ValidationSeverity.Error, path, "ModelId cannot be empty."));
                else if (!seen.Add(model.ModelId))
                    issues.Add(new(ValidationSeverity.Error, path,
                        $"ModelId '{model.ModelId}' is a duplicate within the registry."));
            }
        }

        foreach (var err in Root.Validate())
            issues.Add(new(ValidationSeverity.Error, "PermissionModels", err));

        return issues;
    }
}
