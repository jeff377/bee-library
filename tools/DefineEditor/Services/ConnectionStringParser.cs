using System.Data.Common;
using Bee.Definition.Database;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Result of parsing a vendor-specific connection string into the editor's
/// canonical shape: credential / database-name values extracted into separate
/// fields, with the connection string rewritten to use the framework's
/// <c>{@UserId}</c> / <c>{@Password}</c> / <c>{@DbName}</c> placeholders.
/// </summary>
public sealed record ConnectionStringParseResult(
    string? UserId,
    string? Password,
    string? DbName,
    string RewrittenConnectionString,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors)
{
    public bool IsOk => Errors.Count == 0;
}

/// <summary>
/// Connection-string paste-and-split helper (Phase 5). Zero DB driver
/// dependency — uses <see cref="DbConnectionStringBuilder"/> for tokenisation
/// and a hand-maintained alias table per <see cref="DatabaseType"/> to map
/// credential / database-name keys into the canonical placeholders. Validates
/// dialect consistency by flagging keys that are typical of other databases.
/// </summary>
public static class ConnectionStringParser
{
    private sealed record AliasTable(
        IReadOnlySet<string> UserIdKeys,
        IReadOnlySet<string> PasswordKeys,
        IReadOnlySet<string> DbNameKeys);

    public static ConnectionStringParseResult Parse(string raw, DatabaseType databaseType)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(raw))
        {
            errors.Add("Connection string is empty; nothing to parse.");
            return new(null, null, null, string.Empty, warnings, errors);
        }

        DbConnectionStringBuilder builder;
        try
        {
            builder = new DbConnectionStringBuilder { ConnectionString = raw.Trim() };
        }
        catch (ArgumentException ex)
        {
            errors.Add($"Failed to parse connection string: {ex.Message}");
            return new(null, null, null, string.Empty, warnings, errors);
        }

        var aliases = GetAliasTable(databaseType);
        var foreignAliases = ForeignAliases(databaseType);

        string? userId = null;
        string? password = null;
        string? dbName = null;

        var rewritten = new DbConnectionStringBuilder();
        var seenForeign = new List<string>();

        foreach (var keyObj in builder.Keys!)
        {
            var key = keyObj?.ToString() ?? string.Empty;
            var value = builder[key]?.ToString() ?? string.Empty;

            if (aliases.UserIdKeys.Contains(key))
            {
                userId = value;
                rewritten[key] = "{@UserId}";
            }
            else if (aliases.PasswordKeys.Contains(key))
            {
                password = value;
                rewritten[key] = "{@Password}";
            }
            else if (aliases.DbNameKeys.Contains(key))
            {
                dbName = value;
                rewritten[key] = "{@DbName}";
            }
            else
            {
                rewritten[key] = value;
                if (foreignAliases.Contains(key))
                    seenForeign.Add(key);
            }
        }

        if (IsIntegratedSecurity(builder))
        {
            warnings.Add("Integrated Security / Trusted_Connection detected — credentials will not be extracted.");
        }
        else
        {
            if (userId is null) warnings.Add("UserId key not found — fill it in manually or check the connection string.");
            if (password is null) warnings.Add("Password key not found — fill it in manually or check the connection string.");
        }
        if (dbName is null && aliases.DbNameKeys.Count > 0)
            warnings.Add($"Database-name key not found ({string.Join(" / ", aliases.DbNameKeys)}) — please fill in manually.");

        if (seenForeign.Count > 0)
            warnings.Add($"Detected keys not typical of {databaseType}: {string.Join(", ", seenForeign)} — connection string type may not match.");

        return new ConnectionStringParseResult(
            userId, password, dbName,
            rewritten.ConnectionString,
            warnings, errors);
    }

    /// <summary>
    /// Composes a connection string from a placeholder template plus the
    /// canonical credential / database-name fields. Used by the preview that
    /// shows the user what will be sent to the driver at runtime.
    /// </summary>
    public static string Compose(string template, string? userId, string? password, string? dbName)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        return template
            .Replace("{@UserId}", userId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{@Password}", password ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{@DbName}", dbName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static AliasTable GetAliasTable(DatabaseType type) => type switch
    {
        DatabaseType.SQLServer => new AliasTable(
            UserIdKeys: Set("user id", "uid"),
            PasswordKeys: Set("password", "pwd"),
            DbNameKeys: Set("initial catalog", "database")),
        DatabaseType.PostgreSQL => new AliasTable(
            UserIdKeys: Set("username", "user id"),
            PasswordKeys: Set("password"),
            DbNameKeys: Set("database")),
        DatabaseType.MySQL => new AliasTable(
            UserIdKeys: Set("user id", "uid", "username"),
            PasswordKeys: Set("password", "pwd"),
            DbNameKeys: Set("database")),
        DatabaseType.Oracle => new AliasTable(
            UserIdKeys: Set("user id"),
            PasswordKeys: Set("password"),
            DbNameKeys: Set()), // Oracle EZConnect embeds service in Data Source
        _ => new AliasTable(Set(), Set(), Set()),
    };

    /// <summary>
    /// Keys that strongly identify a different DatabaseType. When seen against
    /// the user-selected type, the parser warns of a possible dialect mismatch.
    /// </summary>
    private static IReadOnlySet<string> ForeignAliases(DatabaseType type) => type switch
    {
        DatabaseType.SQLServer => Set("host", "sslmode", "username"),
        DatabaseType.PostgreSQL => Set("integrated security", "trusted_connection", "initial catalog"),
        DatabaseType.MySQL => Set("integrated security", "trusted_connection", "initial catalog"),
        DatabaseType.Oracle => Set("integrated security", "initial catalog", "database", "host"),
        _ => Set(),
    };

    private static bool IsIntegratedSecurity(DbConnectionStringBuilder builder)
    {
        if (builder.TryGetValue("integrated security", out var s) && IsTruthy(s?.ToString())) return true;
        if (builder.TryGetValue("trusted_connection", out var t) && IsTruthy(t?.ToString())) return true;
        return false;
    }

    private static bool IsTruthy(string? value) =>
        value is not null && (value.Equals("true", StringComparison.OrdinalIgnoreCase)
                              || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                              || value.Equals("sspi", StringComparison.OrdinalIgnoreCase));

    private static IReadOnlySet<string> Set(params string[] keys) =>
        new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
}
