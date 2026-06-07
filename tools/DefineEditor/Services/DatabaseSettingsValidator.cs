using System;
using System.Collections.Generic;
using System.Linq;
using Bee.Definition.Settings;
using Bee.DefineEditor.Models;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Static checks for <see cref="DatabaseSettings"/>. Phase 5 covers four
/// classes: required fields (Server/Item Ids), reference integrity (Item.ServerId
/// must exist), placeholder integrity (when ConnectionString uses
/// <c>{@UserId}</c>/<c>{@Password}</c>/<c>{@DbName}</c>, the corresponding field
/// must be non-empty), and dialect consistency (the connection string should
/// match the selected DatabaseType).
/// </summary>
public static class DatabaseSettingsValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(DatabaseSettings root)
    {
        var issues = new List<ValidationIssue>();

        // Servers
        var serverIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var server in root.Servers ?? Enumerable.Empty<DatabaseServer>())
        {
            var path = string.IsNullOrEmpty(server.Id) ? "Servers[?]" : $"Servers.{server.Id}";
            if (string.IsNullOrWhiteSpace(server.Id))
                issues.Add(new(ValidationSeverity.Error, path, "Server.Id 不可為空。"));
            else if (!serverIds.Add(server.Id))
                issues.Add(new(ValidationSeverity.Error, path, $"Server.Id '{server.Id}' 重複。"));

            ValidatePlaceholders(issues, path, server.ConnectionString, server.UserId, server.Password, dbName: null);
            CheckDialect(issues, path, server.ConnectionString, server.DatabaseType);
        }

        // Items
        var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in root.Items ?? Enumerable.Empty<DatabaseItem>())
        {
            var path = string.IsNullOrEmpty(item.Id) ? "Items[?]" : $"Items.{item.Id}";
            if (string.IsNullOrWhiteSpace(item.Id))
                issues.Add(new(ValidationSeverity.Error, path, "Item.Id 不可為空。"));
            else if (!itemIds.Add(item.Id))
                issues.Add(new(ValidationSeverity.Error, path, $"Item.Id '{item.Id}' 重複。"));

            if (!string.IsNullOrWhiteSpace(item.ServerId) && !serverIds.Contains(item.ServerId))
                issues.Add(new(ValidationSeverity.Error, path,
                    $"Item.ServerId '{item.ServerId}' 在 Servers 內找不到。"));

            if (string.IsNullOrWhiteSpace(item.ServerId)
                && string.IsNullOrWhiteSpace(item.ConnectionString))
            {
                issues.Add(new(ValidationSeverity.Error, path,
                    "Item 必須指定 ServerId 或自行提供 ConnectionString。"));
            }

            ValidatePlaceholders(issues, path, item.ConnectionString, item.UserId, item.Password, item.DbName);
            CheckDialect(issues, path, item.ConnectionString, item.DatabaseType);
        }

        return issues;
    }

    private static void ValidatePlaceholders(
        List<ValidationIssue> issues, string path,
        string connectionString,
        string userId, string password, string? dbName)
    {
        if (string.IsNullOrEmpty(connectionString)) return;
        foreach (var token in ConnectionStringParser.ExtractPlaceholders(connectionString))
        {
            switch (token)
            {
                case "{@UserId}" when string.IsNullOrEmpty(userId):
                    issues.Add(new(ValidationSeverity.Warning, path,
                        "ConnectionString 含 {@UserId} 佔位符，但 UserId 欄位為空（除非 Integrated Security）。"));
                    break;
                case "{@Password}" when string.IsNullOrEmpty(password):
                    issues.Add(new(ValidationSeverity.Warning, path,
                        "ConnectionString 含 {@Password} 佔位符，但 Password 欄位為空。"));
                    break;
                case "{@DbName}" when string.IsNullOrEmpty(dbName):
                    issues.Add(new(ValidationSeverity.Warning, path,
                        "ConnectionString 含 {@DbName} 佔位符，但 DbName 欄位為空。"));
                    break;
            }
        }
    }

    private static void CheckDialect(
        List<ValidationIssue> issues, string path,
        string connectionString,
        Bee.Definition.Database.DatabaseType databaseType)
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        // Lean on the parser's foreign-key heuristic by parsing the composed
        // (placeholder-resolved) form with dummy values — we only care which
        // keys are present, not the resulting credentials.
        var probe = ConnectionStringParser.Compose(connectionString, "x", "x", "x");
        var parsed = ConnectionStringParser.Parse(probe, databaseType);
        foreach (var warning in parsed.Warnings)
            if (warning.Contains("非", StringComparison.Ordinal) && warning.Contains("典型鍵", StringComparison.Ordinal))
                issues.Add(new(ValidationSeverity.Warning, path, warning));
    }
}
