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
                issues.Add(new(ValidationSeverity.Error, path, "Server.Id cannot be empty."));
            else if (!serverIds.Add(server.Id))
                issues.Add(new(ValidationSeverity.Error, path, $"Server.Id '{server.Id}' is a duplicate."));

            ValidatePlaceholders(issues, path, server.ConnectionString, server.UserId, server.Password, dbName: null);
            CheckDialect(issues, path, server.ConnectionString, server.DatabaseType);
        }

        // Items
        var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in root.Items ?? Enumerable.Empty<DatabaseItem>())
        {
            var path = string.IsNullOrEmpty(item.Id) ? "Items[?]" : $"Items.{item.Id}";
            if (string.IsNullOrWhiteSpace(item.Id))
                issues.Add(new(ValidationSeverity.Error, path, "Item.Id cannot be empty."));
            else if (!itemIds.Add(item.Id))
                issues.Add(new(ValidationSeverity.Error, path, $"Item.Id '{item.Id}' is a duplicate."));

            if (!string.IsNullOrWhiteSpace(item.ServerId) && !serverIds.Contains(item.ServerId))
                issues.Add(new(ValidationSeverity.Error, path,
                    $"Item.ServerId '{item.ServerId}' was not found in Servers."));

            if (string.IsNullOrWhiteSpace(item.ServerId)
                && string.IsNullOrWhiteSpace(item.ConnectionString))
            {
                issues.Add(new(ValidationSeverity.Error, path,
                    "Item must specify a ServerId or supply its own ConnectionString."));
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
        if (connectionString.Contains("{@UserId}", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(userId))
            issues.Add(new(ValidationSeverity.Warning, path,
                "ConnectionString contains a {@UserId} placeholder but the UserId field is empty (unless Integrated Security)."));
        if (connectionString.Contains("{@Password}", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(password))
            issues.Add(new(ValidationSeverity.Warning, path,
                "ConnectionString contains a {@Password} placeholder but the Password field is empty."));
        if (connectionString.Contains("{@DbName}", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(dbName))
            issues.Add(new(ValidationSeverity.Warning, path,
                "ConnectionString contains a {@DbName} placeholder but the DbName field is empty."));
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
            if (warning.Contains("not typical of", StringComparison.Ordinal) && warning.Contains("connection string type may not match", StringComparison.Ordinal))
                issues.Add(new(ValidationSeverity.Warning, path, warning));
    }
}
