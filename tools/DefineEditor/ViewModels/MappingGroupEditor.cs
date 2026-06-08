using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Bee.Definition.Forms;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bee.DefineEditor.ViewModels;

/// <summary>
/// View-model shown in the right pane when the selected tree node is a
/// FormSchema relation-group or lookup-group (kind strings
/// <c>FormSchemaKinds.RelationGroup</c> / <c>FormSchemaKinds.LookupGroup</c>).
/// Lets the user pick the target ProgId from solution-wide candidates and
/// edit each FieldMapping inline with destination-field autocomplete drawn
/// from the owning table's fields.
/// </summary>
public sealed partial class MappingGroupEditor : ObservableObject
{
    public FormField Field { get; }
    public bool IsRelation { get; }
    public IReadOnlyList<string> AvailableProgIds { get; }
    public IReadOnlyList<string> DestinationFieldCandidates { get; }
    public ObservableCollection<FieldMapping> Mappings { get; }

    public string PanelTitle => IsRelation ? "Relation field mapping" : "Lookup field mapping";
    public string ProgIdLabel => IsRelation ? "RelationProgId" : "LookupProgId";

    public string ProgIdValue
    {
        get => IsRelation ? Field.RelationProgId : Field.LookupProgId;
        set
        {
            var trimmed = value ?? string.Empty;
            if (IsRelation)
            {
                if (Field.RelationProgId == trimmed) return;
                Field.RelationProgId = trimmed;
            }
            else
            {
                if (Field.LookupProgId == trimmed) return;
                Field.LookupProgId = trimmed;
            }
            OnPropertyChanged();
        }
    }

    public MappingGroupEditor(
        FormField field,
        bool isRelation,
        IReadOnlyList<string> availableProgIds)
    {
        Field = field;
        IsRelation = isRelation;
        AvailableProgIds = availableProgIds;

        var sourceCollection = isRelation
            ? field.RelationFieldMappings
            : field.LookupFieldMappings;
        Mappings = new ObservableCollection<FieldMapping>(
            sourceCollection ?? Enumerable.Empty<FieldMapping>());

        DestinationFieldCandidates = (field.Table?.Fields ?? Enumerable.Empty<FormField>())
            .Select(f => f.FieldName)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray();
    }
}
