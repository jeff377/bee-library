using Bee.Definition;
using Bee.Definition.Forms;
using Bee.DefineEditor.Models;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Static checks over a <see cref="FormSchema"/>: required-field presence, duplicate table and
/// field names, relation and lookup mappings and ProgIds that do not exist in the surrounding
/// solution, list item values, and the unit binding of quantity and weight fields.
/// </summary>
public static class FormSchemaValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(FormSchema schema, SolutionContext context)
    {
        var issues = new List<ValidationIssue>();
        var progIdSet = new HashSet<string>(context.AvailableProgIds, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(schema.ProgId))
            issues.Add(new(ValidationSeverity.Error, "Schema", "ProgId cannot be empty."));

        if (schema.Tables is null || schema.Tables.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, "Schema", "Schema does not declare any FormTable."));
            return issues;
        }

        var seenTableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int ti = 0; ti < schema.Tables.Count; ti++)
        {
            var table = schema.Tables[ti];
            var tablePath = !string.IsNullOrWhiteSpace(table.TableName)
                ? table.TableName
                : $"Tables[{ti}]";

            if (string.IsNullOrWhiteSpace(table.TableName))
                issues.Add(new(ValidationSeverity.Error, tablePath, "TableName cannot be empty."));
            else if (!seenTableNames.Add(table.TableName))
                issues.Add(new(ValidationSeverity.Error, tablePath,
                    $"TableName '{table.TableName}' is a duplicate within the schema."));

            ValidateFields(issues, table, tablePath, progIdSet);
        }

        return issues;
    }

    private static void ValidateFields(
        List<ValidationIssue> issues,
        FormTable table,
        string tablePath,
        HashSet<string> progIdSet)
    {
        if (table.Fields is null || table.Fields.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, tablePath, "FormTable does not declare any FormField."));
            return;
        }

        var seenFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int fi = 0; fi < table.Fields.Count; fi++)
        {
            var field = table.Fields[fi];
            var fieldPath = !string.IsNullOrWhiteSpace(field.FieldName)
                ? $"{tablePath}.{field.FieldName}"
                : $"{tablePath}.Fields[{fi}]";

            if (string.IsNullOrWhiteSpace(field.FieldName))
                issues.Add(new(ValidationSeverity.Error, fieldPath, "FieldName cannot be empty."));
            else if (!seenFieldNames.Add(field.FieldName))
                issues.Add(new(ValidationSeverity.Error, fieldPath,
                    $"FieldName '{field.FieldName}' is a duplicate within '{table.TableName}'."));

            ValidateRelation(issues, field, fieldPath, progIdSet, table);
            ValidateLookup(issues, field, fieldPath, progIdSet, table);
            ValidateListItems(issues, field, fieldPath);
            ValidateUnitBinding(issues, field, fieldPath, table);
        }
    }

    /// <summary>
    /// A quantity or weight field must bind a <see cref="FormField.UnitField"/> that names a field of the
    /// same table. At runtime <c>FormExpressionCalculator</c> reads the unit from the row being computed,
    /// keyed by the declared field name with case-sensitive matching, so a unit field on another table or
    /// with different casing resolves as an empty unit code.
    /// </summary>
    private static void ValidateUnitBinding(
        List<ValidationIssue> issues,
        FormField field,
        string fieldPath,
        FormTable owningTable)
    {
        if (NumberKindProfile.GetDecimalsSource(field.NumberKind) != DecimalsSource.Unit) return;

        var path = $"{fieldPath}.UnitField";
        if (string.IsNullOrWhiteSpace(field.UnitField))
        {
            issues.Add(new(ValidationSeverity.Error, path,
                $"A {field.NumberKind} field requires a UnitField."));
            return;
        }

        var existsOnTable = (owningTable.Fields ?? Enumerable.Empty<FormField>())
            .Any(f => string.Equals(f.FieldName, field.UnitField, StringComparison.Ordinal));
        if (!existsOnTable)
            issues.Add(new(ValidationSeverity.Error, path,
                $"UnitField '{field.UnitField}' does not match a field on '{owningTable.TableName}' (field names are case-sensitive here)."));
    }

    private static void ValidateRelation(
        List<ValidationIssue> issues,
        FormField field,
        string fieldPath,
        HashSet<string> progIdSet,
        FormTable owningTable)
    {
        var hasMappings = field.RelationFieldMappings is { Count: > 0 };
        var hasProgId = !string.IsNullOrWhiteSpace(field.RelationProgId);

        if (hasMappings && !hasProgId)
            issues.Add(new(ValidationSeverity.Error, $"{fieldPath}.Relation",
                "RelationFieldMappings is present but RelationProgId is empty."));

        if (hasProgId && !progIdSet.Contains(field.RelationProgId))
            issues.Add(new(ValidationSeverity.Warning, $"{fieldPath}.Relation",
                $"RelationProgId '{field.RelationProgId}' was not found in the current solution."));

        if (!hasMappings) return;
        var localFieldNames = (owningTable.Fields ?? Enumerable.Empty<FormField>())
            .Select(f => f.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenDest = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int mi = 0; mi < field.RelationFieldMappings!.Count; mi++)
        {
            var mapping = field.RelationFieldMappings[mi];
            var path = $"{fieldPath}.Relation[{mi}]";
            if (string.IsNullOrWhiteSpace(mapping.SourceField))
                issues.Add(new(ValidationSeverity.Error, path, "SourceField cannot be empty."));
            if (string.IsNullOrWhiteSpace(mapping.DestinationField))
                issues.Add(new(ValidationSeverity.Error, path, "DestinationField cannot be empty."));
            else
            {
                if (!localFieldNames.Contains(mapping.DestinationField))
                    issues.Add(new(ValidationSeverity.Error, path,
                        $"DestinationField '{mapping.DestinationField}' does not exist on '{owningTable.TableName}'."));
                if (!seenDest.Add(mapping.DestinationField))
                    issues.Add(new(ValidationSeverity.Error, path,
                        $"DestinationField '{mapping.DestinationField}' is a duplicate within the same RelationFieldMappings group."));
            }
        }
    }

    private static void ValidateLookup(
        List<ValidationIssue> issues,
        FormField field,
        string fieldPath,
        HashSet<string> progIdSet,
        FormTable owningTable)
    {
        var hasMappings = field.LookupFieldMappings is { Count: > 0 };
        var hasProgId = !string.IsNullOrWhiteSpace(field.LookupProgId);

        if (hasMappings && !hasProgId)
            issues.Add(new(ValidationSeverity.Error, $"{fieldPath}.Lookup",
                "LookupFieldMappings is present but LookupProgId is empty."));

        if (hasProgId && !progIdSet.Contains(field.LookupProgId))
            issues.Add(new(ValidationSeverity.Warning, $"{fieldPath}.Lookup",
                $"LookupProgId '{field.LookupProgId}' was not found in the current solution."));

        if (!hasMappings) return;
        var localFieldNames = (owningTable.Fields ?? Enumerable.Empty<FormField>())
            .Select(f => f.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int mi = 0; mi < field.LookupFieldMappings!.Count; mi++)
        {
            var mapping = field.LookupFieldMappings[mi];
            var path = $"{fieldPath}.Lookup[{mi}]";
            if (string.IsNullOrWhiteSpace(mapping.SourceField))
                issues.Add(new(ValidationSeverity.Error, path, "SourceField cannot be empty."));
            if (string.IsNullOrWhiteSpace(mapping.DestinationField))
                issues.Add(new(ValidationSeverity.Error, path, "DestinationField cannot be empty."));
            else if (!localFieldNames.Contains(mapping.DestinationField))
                issues.Add(new(ValidationSeverity.Error, path,
                    $"DestinationField '{mapping.DestinationField}' does not exist on '{owningTable.TableName}'."));
        }
    }

    private static void ValidateListItems(List<ValidationIssue> issues, FormField field, string fieldPath)
    {
        if (field.ListItems is not { Count: > 0 }) return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < field.ListItems.Count; i++)
        {
            var item = field.ListItems[i];
            var path = $"{fieldPath}.ListItems[{i}]";
            if (string.IsNullOrWhiteSpace(item.Value))
                issues.Add(new(ValidationSeverity.Error, path, "ListItem.Value cannot be empty."));
            else if (!seen.Add(item.Value))
                issues.Add(new(ValidationSeverity.Error, path,
                    $"ListItem.Value '{item.Value}' is a duplicate within the same field."));
        }
    }
}
