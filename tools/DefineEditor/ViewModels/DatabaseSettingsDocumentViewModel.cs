using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Services;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Editor for <see cref="DatabaseSettings"/>. Two top-level groups (Servers /
/// Items); selection on a Server or Item yields a wrapper editor with proxy
/// fields plus the connection-string paste-and-split UI. Validation runs the
/// dedicated <see cref="DatabaseSettingsValidator"/>.
/// </summary>
public sealed partial class DatabaseSettingsDocumentViewModel : SingletonDocumentViewModelBase
{
    public const string KindRoot = "DatabaseSettings";
    public const string KindServersGroup = "ServersGroup";
    public const string KindItemsGroup = "ItemsGroup";
    public const string KindServer = "DatabaseServer";
    public const string KindItem = "DatabaseItem";

    public DatabaseSettings Root { get; }

    protected override object RootObject => Root;

    public override string TabIcon => "DefDatabaseSettings";

    public bool SelectedKindIsServersGroup => SelectedTreeNode?.Kind == KindServersGroup;
    public bool SelectedKindIsItemsGroup => SelectedTreeNode?.Kind == KindItemsGroup;

    protected override bool HasVisibleAddMenuItems =>
        SelectedKindIsServersGroup || SelectedKindIsItemsGroup;

    protected override void OnSelectedTreeNodeRefreshDerivedProperties(SettingsTreeNode? value)
    {
        OnPropertyChanged(nameof(SelectedKindIsServersGroup));
        OnPropertyChanged(nameof(SelectedKindIsItemsGroup));
    }

    public override object? SelectedEditorContext => SelectedTreeNode switch
    {
        { Kind: KindServer, Payload: DatabaseServer server } =>
            new DatabaseServerEditor(server, () => IsDirty = true),
        { Kind: KindItem, Payload: DatabaseItem item } =>
            new DatabaseItemEditor(item, SnapshotServerIds(), () => IsDirty = true),
        _ => SelectedTreeNode?.Payload,
    };

    private DatabaseSettingsDocumentViewModel(string filePath, DatabaseSettings root)
        : base(filePath, "DatabaseSettings", keyText: string.Empty)
    {
        Root = root;
        Roots.Add(BuildRootNode(root));
        SelectedTreeNode = Roots[0];
    }

    public static DatabaseSettingsDocumentViewModel Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("DatabaseSettings file not found.", filePath);
        var root = XmlCodec.DeserializeFromFile<DatabaseSettings>(filePath)
            ?? throw new InvalidOperationException($"DatabaseSettings deserialized to null: {filePath}");
        return new DatabaseSettingsDocumentViewModel(filePath, root);
    }

    private static SettingsTreeNode BuildRootNode(DatabaseSettings root)
    {
        var rootNode = SettingsTreeNode.Create("DefDatabaseSettings", KindRoot, root, RefreshRoot, isExpanded: true);

        var serversGroup = SettingsTreeNode.Create("IconServer", KindServersGroup, root, RefreshServersGroup, isExpanded: true);
        if (root.Servers is { } servers)
            foreach (var server in servers)
                serversGroup.AddChild(BuildServerNode(server));
        rootNode.AddChild(serversGroup);

        var itemsGroup = SettingsTreeNode.Create("IconDatabase", KindItemsGroup, root, RefreshItemsGroup, isExpanded: true);
        if (root.Items is { } items)
            foreach (var item in items)
                itemsGroup.AddChild(BuildItemNode(item));
        rootNode.AddChild(itemsGroup);

        return rootNode;
    }

    private static SettingsTreeNode BuildServerNode(DatabaseServer server) =>
        SettingsTreeNode.Create("IconServer", KindServer, server, RefreshServer, isExpanded: false);

    private static SettingsTreeNode BuildItemNode(DatabaseItem item) =>
        SettingsTreeNode.Create("IconDatabase", KindItem, item, RefreshItem, isExpanded: false);

    private static void RefreshRoot(SettingsTreeNode node)
    {
        var r = (DatabaseSettings)node.Payload!;
        node.Header = "DatabaseSettings";
        node.Detail = $"{r.Servers?.Count ?? 0} Server(s) / {r.Items?.Count ?? 0} Item(s)";
    }

    private static void RefreshServersGroup(SettingsTreeNode node)
    {
        var r = (DatabaseSettings)node.Payload!;
        node.Header = $"Servers ({r.Servers?.Count ?? 0})";
        node.Detail = "Database servers (provide shared connection-string templates).";
    }

    private static void RefreshItemsGroup(SettingsTreeNode node)
    {
        var r = (DatabaseSettings)node.Payload!;
        node.Header = $"Items ({r.Items?.Count ?? 0})";
        node.Detail = "Database items (bind a Server or supply their own connection string; pin CategoryId / DbName).";
    }

    private static void RefreshServer(SettingsTreeNode node)
    {
        var s = (DatabaseServer)node.Payload!;
        node.Header = $"{s.Id}  —  {s.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"Id：{s.Id}",
            $"DisplayName：{s.DisplayName}",
            $"DatabaseType：{s.DatabaseType}",
            $"UserId：{s.UserId}",
            $"Password：{(string.IsNullOrEmpty(s.Password) ? "(empty)" : "******")}",
            $"ConnectionString：{s.ConnectionString}");
    }

    private static void RefreshItem(SettingsTreeNode node)
    {
        var i = (DatabaseItem)node.Payload!;
        node.Header = $"{i.Id}  —  {i.DisplayName}";
        node.Detail = string.Join(Environment.NewLine,
            $"Id：{i.Id}",
            $"CategoryId：{i.CategoryId}",
            $"DatabaseType：{i.DatabaseType}",
            $"ServerId：{(string.IsNullOrEmpty(i.ServerId) ? "(none, supplies own connection string)" : i.ServerId)}",
            $"DbName：{i.DbName}",
            $"UserId：{i.UserId}",
            $"Password：{(string.IsNullOrEmpty(i.Password) ? "(empty)" : "******")}",
            $"ConnectionString：{i.ConnectionString}");
    }

    [RelayCommand(CanExecute = nameof(CanAddServer))]
    private void AddServer()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindServersGroup)
                        ?? Roots[0].Children.FirstOrDefault(c => c.Kind == KindServersGroup);
        if (groupNode is null) return;
        var id = UniqueKey(Root.Servers!.Select(s => s.Id), "new_server");
        var server = new DatabaseServer { Id = id, DisplayName = "New server", DatabaseType = DatabaseType.SQLServer };
        Root.Servers!.Add(server);
        var node = BuildServerNode(server);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "DatabaseServer", id);
    }

    private bool CanAddServer() => SelectedTreeNode is not null;

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    private void AddItem()
    {
        var groupNode = FindAncestor(SelectedTreeNode, KindItemsGroup)
                        ?? Roots[0].Children.FirstOrDefault(c => c.Kind == KindItemsGroup);
        if (groupNode is null) return;
        var id = UniqueKey(Root.Items!.Select(i => i.Id), "new_item");
        var item = new DatabaseItem
        {
            Id = id,
            DisplayName = "New database",
            DatabaseType = DatabaseType.SQLServer,
        };
        Root.Items!.Add(item);
        var node = BuildItemNode(item);
        groupNode.AddChild(node);
        groupNode.IsExpanded = true;
        SelectedTreeNode = node;
        IsDirty = true;
        StatusText = L("Status_AddedNamed", "DatabaseItem", id);
    }

    private bool CanAddItem() => SelectedTreeNode is not null;

    protected override Action? GetDeleteAction(SettingsTreeNode node) => node.Kind switch
    {
        KindServer when node.Payload is DatabaseServer s => () => Root.Servers!.Remove(s),
        KindItem when node.Payload is DatabaseItem i => () => Root.Items!.Remove(i),
        _ => null,
    };

    protected override IReadOnlyList<ValidationIssue> PerformValidation() =>
        DatabaseSettingsValidator.Validate(Root);

    private IReadOnlyList<string> SnapshotServerIds() =>
        (Root.Servers ?? Enumerable.Empty<DatabaseServer>())
            .Select(s => s.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToArray();
}
