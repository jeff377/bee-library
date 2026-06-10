using Bee.Base.Serialization;
using Bee.Definition.Collections;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.DefineEditor.Models;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="SystemSettings"/>. The 5 Configuration sub-objects and
/// the ExtendedProperties bag are mounted as fixed tree nodes; BackendConfiguration
/// also exposes its 4 nested option records (LogOptions, SecurityKeySettings,
/// Components, CacheNotifyOptions) as children. Add/Delete is only meaningful on
/// the ExtendedProperties branch.
/// </summary>
public sealed partial class SystemSettingsDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "SystemSettings";
    public const string KindCommon = "CommonConfiguration";
    public const string KindBackend = "BackendConfiguration";
    public const string KindLogOptions = "LogOptions";
    public const string KindSecurityKeys = "SecurityKeySettings";
    public const string KindBackendComponents = "BackendComponents";
    public const string KindCacheNotify = "CacheNotifyOptions";
    public const string KindFrontend = "FrontendConfiguration";
    public const string KindWebsite = "WebsiteConfiguration";
    public const string KindBackgroundService = "BackgroundServiceConfiguration";
    public const string KindExtendedGroup = "ExtendedProperties";
    public const string KindProperty = "Property";

    public SystemSettings Root { get; }

    protected override object RootObject => Root;

    public override string TabIcon => "DefSystemSettings";

    public bool SelectedKindIsExtendedGroup => SelectedTreeNode?.Kind == KindExtendedGroup;

    protected override bool HasVisibleAddMenuItems => SelectedKindIsExtendedGroup;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsExtendedGroup));
    }

    private SystemSettingsDocumentViewModel(string filePath, SystemSettings root)
        : base(filePath, "SystemSettings", keyText: string.Empty)
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static SystemSettingsDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("SystemSettings file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<SystemSettings>(filePath)
            ?? throw new InvalidOperationException($"SystemSettings deserialized to null: {filePath}");
        return new SystemSettingsDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(SystemSettings root)
    {
        var node = SettingsTreeNode.Create("DefSystemSettings", KindRoot, root, RefreshRoot, isExpanded: true);

        node.AddChild(SettingsTreeNode.Create("IconSettings", KindCommon, root.CommonConfiguration, RefreshCommon, isExpanded: false));

        var backendNode = SettingsTreeNode.Create("IconWrench", KindBackend, root.BackendConfiguration, RefreshBackend, isExpanded: false);
        backendNode.AddChild(SettingsTreeNode.Create("IconText", KindLogOptions, root.BackendConfiguration.LogOptions, RefreshLogOptions, isExpanded: false));
        backendNode.AddChild(SettingsTreeNode.Create("IconLock", KindSecurityKeys, root.BackendConfiguration.SecurityKeySettings, RefreshSecurityKeys, isExpanded: false));
        backendNode.AddChild(SettingsTreeNode.Create("IconLayers", KindBackendComponents, root.BackendConfiguration.Components, RefreshBackendComponents, isExpanded: false));
        backendNode.AddChild(SettingsTreeNode.Create("IconBell", KindCacheNotify, root.BackendConfiguration.CacheNotifyOptions, RefreshCacheNotify, isExpanded: false));
        node.AddChild(backendNode);

        node.AddChild(SettingsTreeNode.Create("IconMonitor", KindFrontend, root.FrontendConfiguration, RefreshFrontend, isExpanded: false));
        node.AddChild(SettingsTreeNode.Create("IconGlobe", KindWebsite, root.WebsiteConfiguration, RefreshWebsite, isExpanded: false));
        node.AddChild(SettingsTreeNode.Create("IconClock", KindBackgroundService, root.BackgroundServiceConfiguration, RefreshBackgroundService, isExpanded: false));

        var extGroup = SettingsTreeNode.Create("IconList", KindExtendedGroup, root, RefreshExtendedGroup, isExpanded: false);
        if (root.ExtendedProperties is { } props)
            foreach (var p in props)
                extGroup.AddChild(SettingsTreeNode.Create("IconDot", KindProperty, p, RefreshProperty, isExpanded: false));
        node.AddChild(extGroup);

        return node;
    }

    private static void RefreshRoot(SettingsTreeNode node)
    {
        node.Header = "SystemSettings";
        node.Detail = "System parameters and environment settings";
    }

    private static void RefreshCommon(SettingsTreeNode node)
    {
        var c = (CommonConfiguration)node.Payload!;
        node.Header = "CommonConfiguration";
        node.Detail = string.Join(Environment.NewLine,
            $"Version：{c.Version}",
            $"IsDebugMode：{c.IsDebugMode}",
            $"DefaultLang：{c.DefaultLang}",
            $"AllowedTypeNamespaces：{c.AllowedTypeNamespaces}");
    }

    private static void RefreshBackend(SettingsTreeNode node)
    {
        node.Header = "BackendConfiguration";
        node.Detail = "Backend parameters (edit each block via the child nodes below)";
    }

    private static void RefreshLogOptions(SettingsTreeNode node)
    {
        node.Header = "LogOptions";
        node.Detail = "(no editable fields)";
    }

    private static void RefreshSecurityKeys(SettingsTreeNode node)
    {
        var s = (SecurityKeySettings)node.Payload!;
        node.Header = "SecurityKeySettings";
        node.Detail = string.Join(Environment.NewLine,
            $"ApiEncryptionKey：{Mask(s.ApiEncryptionKey)}",
            $"CookieEncryptionKey：{Mask(s.CookieEncryptionKey)}",
            $"ConfigEncryptionKey：{Mask(s.ConfigEncryptionKey)}",
            $"DatabaseEncryptionKey：{Mask(s.DatabaseEncryptionKey)}");
    }

    private static void RefreshBackendComponents(SettingsTreeNode node)
    {
        node.Header = "BackendComponents";
        node.Detail = $"12 component types; defaults come from BackendDefaultTypes (CacheProvider etc.)";
    }

    private static void RefreshCacheNotify(SettingsTreeNode node)
    {
        var c = (CacheNotifyOptions)node.Payload!;
        node.Header = "CacheNotifyOptions";
        node.Detail = string.Join(Environment.NewLine,
            $"Enabled：{c.Enabled}",
            $"IntervalSeconds：{c.IntervalSeconds}",
            $"MarginSeconds：{c.MarginSeconds}",
            $"DatabaseId：{c.DatabaseId}");
    }

    private static void RefreshFrontend(SettingsTreeNode node)
    {
        node.Header = "FrontendConfiguration";
        node.Detail = "(no editable fields)";
    }

    private static void RefreshWebsite(SettingsTreeNode node)
    {
        node.Header = "WebsiteConfiguration";
        node.Detail = "(no editable fields)";
    }

    private static void RefreshBackgroundService(SettingsTreeNode node)
    {
        node.Header = "BackgroundServiceConfiguration";
        node.Detail = "(no editable fields)";
    }

    private static void RefreshExtendedGroup(SettingsTreeNode node)
    {
        var root = (SystemSettings)node.Payload!;
        node.Header = "ExtendedProperties";
        node.Detail = $"{root.ExtendedProperties?.Count ?? 0} extended property entries";
    }

    private static void RefreshProperty(SettingsTreeNode node)
    {
        var p = (Property)node.Payload!;
        node.Header = $"{p.Name}  =  {Mask(p.Value)}";
        node.Detail = string.Join(Environment.NewLine,
            $"Name：{p.Name}",
            $"Value：{p.Value}");
    }

    private static string Mask(string s) =>
        string.IsNullOrEmpty(s) ? "(empty)" : (s.Length > 30 ? s[..30] + "…" : s);

    [RelayCommand(CanExecute = nameof(CanAddProperty))]
    private void AddProperty()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindExtendedGroup)
                        ?? (SelectedTreeNode?.Kind == KindExtendedGroup ? SelectedTreeNode : null);
        if (groupNode is null) return;

        var name = UniqueKey(
            (Root.ExtendedProperties ?? new PropertyCollection()).Select(p => p.Name),
            "NewProperty");
        var prop = new Property { Name = name, Value = string.Empty };
        Root.ExtendedProperties!.Add(prop);
        var node = SettingsTreeNode.Create("IconDot", KindProperty, prop, RefreshProperty, isExpanded: false);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "ExtendedProperty", name);
    }

    private bool CanAddProperty() =>
        SelectedTreeNode?.Kind == KindExtendedGroup
        || FindAncestor(SelectedTreeNode, KindExtendedGroup) is not null;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindProperty when node.Payload is Property p =>
            () => Root.ExtendedProperties!.Remove(p),
        _ => null,
    };
}
