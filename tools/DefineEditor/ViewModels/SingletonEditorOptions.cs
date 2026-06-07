using System;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Sorting;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Static enum sources for ComboBox bindings inside the singleton settings
/// editors. PermissionAction excludes <c>None</c> because the editor never
/// produces a no-op rule.
/// </summary>
public static class SingletonEditorOptions
{
    public static PermissionAction[] PermissionActions { get; } =
    {
        PermissionAction.Create,
        PermissionAction.Read,
        PermissionAction.Update,
        PermissionAction.Delete,
        PermissionAction.Print,
        PermissionAction.Export,
    };

    public static ScopeStrategy[] ScopeStrategies { get; } = Enum.GetValues<ScopeStrategy>();

    public static DatabaseType[] DatabaseTypes { get; } = Enum.GetValues<DatabaseType>();

    public static FieldDbType[] FieldDbTypes { get; } = Enum.GetValues<FieldDbType>();

    public static ControlType[] ControlTypes { get; } = Enum.GetValues<ControlType>();

    public static SortDirection[] SortDirections { get; } = Enum.GetValues<SortDirection>();

    public static GridControlAllowActions[] GridAllowActions { get; } =
        Enum.GetValues<GridControlAllowActions>();
}
