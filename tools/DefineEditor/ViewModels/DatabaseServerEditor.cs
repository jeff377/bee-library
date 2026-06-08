using System;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.DefineEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Wrapper for a selected <see cref="DatabaseServer"/> shown in the editor's
/// right pane. Proxies the server's scalar fields with INPC so TextBox bindings
/// see updates from <see cref="ApplyCommand"/>, and adds the paste/parse/preview
/// state used by the connection-string splitter.
/// </summary>
public sealed partial class DatabaseServerEditor : ObservableObject
{
    public DatabaseServer Server { get; }

    private readonly Action _markDirty;

    public DatabaseServerEditor(DatabaseServer server, Action markDirty)
    {
        Server = server;
        _markDirty = markDirty;
    }

    public string Id
    {
        get => Server.Id;
        set
        {
            if (Server.Id == value) return;
            Server.Id = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    public string DisplayName
    {
        get => Server.DisplayName;
        set
        {
            if (Server.DisplayName == value) return;
            Server.DisplayName = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    public DatabaseType DatabaseType
    {
        get => Server.DatabaseType;
        set
        {
            if (Server.DatabaseType == value) return;
            Server.DatabaseType = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    public string ConnectionString
    {
        get => Server.ConnectionString;
        set
        {
            if (Server.ConnectionString == value) return;
            Server.ConnectionString = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    public string UserId
    {
        get => Server.UserId;
        set
        {
            if (Server.UserId == value) return;
            Server.UserId = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    public string Password
    {
        get => Server.Password;
        set
        {
            if (Server.Password == value) return;
            Server.Password = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    // --- Paste / parse / preview state (does NOT mark dirty) ---

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
