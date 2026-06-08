using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.DefineEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Wrapper for a selected <see cref="DatabaseItem"/> shown in the editor's
/// right pane. Same shape as <see cref="DatabaseServerEditor"/> plus
/// CategoryId / ServerId / DbName fields and a snapshot of available
/// ServerId candidates for the dropdown.
/// </summary>
public sealed partial class DatabaseItemEditor : ObservableObject
{
    public DatabaseItem Item { get; }

    public IReadOnlyList<string> AvailableServerIds { get; }

    private readonly Action _markDirty;

    public DatabaseItemEditor(DatabaseItem item, IReadOnlyList<string> availableServerIds, Action markDirty)
    {
        Item = item;
        AvailableServerIds = availableServerIds;
        _markDirty = markDirty;
    }

    public string Id
    {
        get => Item.Id;
        set { if (Item.Id == value) return; Item.Id = value; OnPropertyChanged(); _markDirty(); }
    }

    public string CategoryId
    {
        get => Item.CategoryId;
        set { if (Item.CategoryId == value) return; Item.CategoryId = value; OnPropertyChanged(); _markDirty(); }
    }

    public string DisplayName
    {
        get => Item.DisplayName;
        set { if (Item.DisplayName == value) return; Item.DisplayName = value; OnPropertyChanged(); _markDirty(); }
    }

    public DatabaseType DatabaseType
    {
        get => Item.DatabaseType;
        set { if (Item.DatabaseType == value) return; Item.DatabaseType = value; OnPropertyChanged(); _markDirty(); }
    }

    public string ServerId
    {
        get => Item.ServerId;
        set { if (Item.ServerId == value) return; Item.ServerId = value ?? string.Empty; OnPropertyChanged(); _markDirty(); }
    }

    public string ConnectionString
    {
        get => Item.ConnectionString;
        set { if (Item.ConnectionString == value) return; Item.ConnectionString = value; OnPropertyChanged(); _markDirty(); }
    }

    public string DbName
    {
        get => Item.DbName;
        set { if (Item.DbName == value) return; Item.DbName = value; OnPropertyChanged(); _markDirty(); }
    }

    public string UserId
    {
        get => Item.UserId;
        set { if (Item.UserId == value) return; Item.UserId = value; OnPropertyChanged(); _markDirty(); }
    }

    public string Password
    {
        get => Item.Password;
        set { if (Item.Password == value) return; Item.Password = value; OnPropertyChanged(); _markDirty(); }
    }

    [ObservableProperty] private string _pasteInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasParseResult))]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private ConnectionStringParseResult? _parseResult;

    public bool HasParseResult => ParseResult is not null;

    [RelayCommand]
    private void Parse()
    {
        ParseResult = ConnectionStringParser.Parse(PasteInput, DatabaseType);
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        if (ParseResult is null || !ParseResult.IsOk) return;
        ConnectionString = ParseResult.RewrittenConnectionString;
        if (ParseResult.UserId is not null) UserId = ParseResult.UserId;
        if (ParseResult.Password is not null) Password = ParseResult.Password;
        if (ParseResult.DbName is not null) DbName = ParseResult.DbName;
        ParseResult = null;
        PasteInput = string.Empty;
    }

    private bool CanApply() => ParseResult is { IsOk: true };

    [RelayCommand]
    private void Clear()
    {
        PasteInput = string.Empty;
        ParseResult = null;
    }
}
