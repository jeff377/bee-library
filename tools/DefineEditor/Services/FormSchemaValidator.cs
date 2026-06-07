using System;
using System.Collections.Generic;
using System.Linq;
using Bee.Definition.Forms;
using Bee.DefineEditor.Models;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Static checks over a <see cref="FormSchema"/>. Phase 3 covers the three
/// classes called out in the plan: required-field presence, duplicate field
/// names within a table, and relation/lookup ProgIds that do not exist in the
/// surrounding solution.
/// </summary>
public static class FormSchemaValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(FormSchema schema, SolutionContext context)
    {
        var issues = new List<ValidationIssue>();
        var progIdSet = new HashSet<string>(context.AvailableProgIds, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(schema.ProgId))
            issues.Add(new(ValidationSeverity.Error, "Schema", "ProgId 不可為空。"));

        if (schema.Tables is null || schema.Tables.Count == 0)
        {
            issues.Add(new(ValidationSeverity.Warning, "Schema", "Schema 尚未定義任何 FormTable。"));
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
                issues.Add(new(ValidationSeverity.Error, tablePath, "TableName 不可為空。"));
            else if (!seenTableNames.Add(table.TableName))
                issues.Add(new(ValidationSeverity.Error, tablePath,
                    $"TableName '{table.TableName}' 在 schema 內重複。"));

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
            issues.Add(new(ValidationSeverity.Warning, tablePath, "FormTable 尚未定義任何 FormField。"));
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
                issues.Add(new(ValidationSeverity.Error, fieldPath, "FieldName 不可為空。"));
            else if (!seenFieldNames.Add(field.FieldName))
                issues.Add(new(ValidationSeverity.Error, fieldPath,
                    $"FieldName '{field.FieldName}' 在 '{table.TableName}' 內重複。"));

            ValidateRelation(issues, field, fieldPath, progIdSet, table);
            ValidateLookup(issues, field, fieldPath, progIdSet, table);
            ValidateListItems(issues, field, fieldPath);
        }
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
                "存在 RelationFieldMappings 但 RelationProgId 為空。"));

        if (hasProgId && !progIdSet.Contains(field.RelationProgId))
            issues.Add(new(ValidationSeverity.Warning, $"{fieldPath}.Relation",
                $"RelationProgId '{field.RelationProgId}' 在目前方案內找不到。"));

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
                issues.Add(new(ValidationSeverity.Error, path, "SourceField 不可為空。"));
            if (string.IsNullOrWhiteSpace(mapping.DestinationField))
                issues.Add(new(ValidationSeverity.Error, path, "DestinationField 不可為空。"));
            else
            {
                if (!localFieldNames.Contains(mapping.DestinationField))
                    issues.Add(new(ValidationSeverity.Error, path,
                        $"DestinationField '{mapping.DestinationField}' 不存在於 '{owningTable.TableName}'。"));
                if (!seenDest.Add(mapping.DestinationField))
                    issues.Add(new(ValidationSeverity.Error, path,
                        $"DestinationField '{mapping.DestinationField}' 在同一組 RelationFieldMappings 內重複。"));
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
                "存在 LookupFieldMappings 但 LookupProgId 為空。"));

        if (hasProgId && !progIdSet.Contains(field.LookupProgId))
            issues.Add(new(ValidationSeverity.Warning, $"{fieldPath}.Lookup",
                $"LookupProgId '{field.LookupProgId}' 在目前方案內找不到。"));

        if (!hasMappings) return;
        var localFieldNames = (owningTable.Fields ?? Enumerable.Empty<FormField>())
            .Select(f => f.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (int mi = 0; mi < field.LookupFieldMappings!.Count; mi++)
        {
            var mapping = field.LookupFieldMappings[mi];
            var path = $"{fieldPath}.Lookup[{mi}]";
            if (string.IsNullOrWhiteSpace(mapping.SourceField))
                issues.Add(new(ValidationSeverity.Error, path, "SourceField 不可為空。"));
            if (string.IsNullOrWhiteSpace(mapping.DestinationField))
                issues.Add(new(ValidationSeverity.Error, path, "DestinationField 不可為空。"));
            else if (!localFieldNames.Contains(mapping.DestinationField))
                issues.Add(new(ValidationSeverity.Error, path,
                    $"DestinationField '{mapping.DestinationField}' 不存在於 '{owningTable.TableName}'。"));
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
                issues.Add(new(ValidationSeverity.Error, path, "ListItem.Value 不可為空。"));
            else if (!seen.Add(item.Value))
                issues.Add(new(ValidationSeverity.Error, path,
                    $"ListItem.Value '{item.Value}' 在同一欄位內重複。"));
        }
    }
}
