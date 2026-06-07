using System;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// Read-only enum sources for ComboBox bindings inside the FormSchema editor.
/// Exposed as static arrays so axaml can bind via <c>x:Static</c> without
/// reaching through a view-model instance.
/// </summary>
public static class FormSchemaEditorOptions
{
    public static FieldDbType[] DbTypes { get; } = Enum.GetValues<FieldDbType>();
    public static FieldType[] FieldTypes { get; } = Enum.GetValues<FieldType>();
    public static ControlType[] ControlTypes { get; } = Enum.GetValues<ControlType>();
    public static ScopeRole[] ScopeRoles { get; } = Enum.GetValues<ScopeRole>();
}
