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
        var node = MakeNode("🛡️", KindRoot, root, RefreshRoot, isExpanded: true);
        if (root.Models is { } models)
            foreach (var model in models)
                node.AddChild(BuildModelNode(model));
        return node;
    }

    private static SettingsTreeNode BuildModelNode(PermissionModel model)
    {
        var node = MakeNode("📦", KindModel, model, RefreshModel, isExpanded: false);
        if (model.Rules is { } rules)
            foreach (var rule in rules)
                node.AddChild(BuildRuleNode(rule));
        return node;
    }

    private static SettingsTreeNode BuildRuleNode(PermissionRule rule) =>
        MakeNode("🔑", KindRule, rule, RefreshRule, isExpanded: false);

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var root = (PermissionModels)node.Payload!;
        node.Header = "PermissionModels";
        node.Detail = $"共 {root.Models?.Count ?? 0} 個 PermissionModel";
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

    private static SettingsTreeNode MakeNode(
        string icon, string kind, object payload,
        Action<SettingsTreeNode> refresher, bool isExpanded)
    {
        var node = new SettingsTreeNode
        {
            Icon = icon,
            Kind = kind,
            Payload = payload,
            IsExpanded = isExpanded,
            Refresher = refresher,
        };
        node.RefreshDisplay();
        return node;
    }

    [RelayCommand(CanExecute = nameof(CanAddModel))]
    private void AddModel()
    {
        if (SelectedTreeNode is not { Kind: KindRoot, Payload: PermissionModels root } rootNode)
            return;
        var modelId = UniqueKey(root.Models!.Select(m => m.ModelId), "NewModel");
        var model = new PermissionModel(modelId, "新模型");
        root.Models!.Add(model);
        var node = BuildModelNode(model);
        rootNode.AddChild(node);
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = $"已新增 PermissionModel：{modelId}（尚未存檔）";
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
            StatusText = "此模型已涵蓋所有 PermissionAction，無法再新增 Rule。";
            return;
        }
        var rule = new PermissionRule(action.Value);
        model.Rules!.Add(rule);
        var node = BuildRuleNode(rule);
        modelNode.AddChild(node);
        modelNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = $"已新增 Rule：{action.Value}（尚未存檔）";
    }

    private bool CanAddRule() => FindAncestor(SelectedTreeNode, KindModel) is not null;

    private static PermissionAction? PickAvailableAction(PermissionModel model)
    {
        var taken = new HashSet<PermissionAction>(
            (model.Rules ?? Enumerable.Empty<PermissionRule>()).Select(r => r.Action));
        foreach (var candidate in SingletonEditorOptions.PermissionActions)
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
            issues.Add(new(ValidationSeverity.Warning, "PermissionModels", "尚未定義任何 PermissionModel。"));
        }
        else
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var model in models)
            {
                var path = !string.IsNullOrWhiteSpace(model.ModelId) ? model.ModelId : "(unnamed)";
                if (string.IsNullOrWhiteSpace(model.ModelId))
                    issues.Add(new(ValidationSeverity.Error, path, "ModelId 不可為空。"));
                else if (!seen.Add(model.ModelId))
                    issues.Add(new(ValidationSeverity.Error, path,
                        $"ModelId '{model.ModelId}' 在 registry 內重複。"));
            }
        }

        foreach (var err in Root.Validate())
            issues.Add(new(ValidationSeverity.Error, "PermissionModels", err));

        return issues;
    }
}
