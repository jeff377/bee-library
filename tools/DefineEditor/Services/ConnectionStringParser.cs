using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
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
        IReadOnlySet<string> DbNameKeys,
        IReadOnlySet<string> KnownOtherKeys);

    public static ConnectionStringParseResult Parse(string raw, DatabaseType databaseType)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(raw))
        {
            errors.Add("連線字串為空，無法拆解。");
            return new(null, null, null, string.Empty, warnings, errors);
        }

        DbConnectionStringBuilder builder;
        try
        {
            builder = new DbConnectionStringBuilder { ConnectionString = raw.Trim() };
        }
        catch (ArgumentException ex)
        {
            errors.Add($"連線字串格式無法解析：{ex.Message}");
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
            warnings.Add("偵測到 Integrated Security / Trusted_Connection 設定 — 不會拆出帳號密碼。");
        }
        else
        {
            if (userId is null) warnings.Add("找不到 UserId 鍵 — 請手動填入或檢查連線字串。");
            if (password is null) warnings.Add("找不到 Password 鍵 — 請手動填入或檢查連線字串。");
        }
        if (dbName is null && aliases.DbNameKeys.Count > 0)
            warnings.Add($"找不到資料庫名稱鍵（{string.Join(" / ", aliases.DbNameKeys)}）— 請手動填入。");

        if (seenForeign.Count > 0)
            warnings.Add($"偵測到非 {databaseType} 典型鍵：{string.Join(", ", seenForeign)} — 連線字串型別可能不符。");

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

    /// <summary>
    /// Returns every <c>{@Placeholder}</c> token referenced in <paramref name="connectionString"/>.
    /// Used by the validator to check that referenced placeholders have non-empty values.
    /// </summary>
    public static IReadOnlyList<string> ExtractPlaceholders(string connectionString)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(connectionString)) return list;
        ScanPlaceholder(connectionString, "{@UserId}", list);
        ScanPlaceholder(connectionString, "{@Password}", list);
        ScanPlaceholder(connectionString, "{@DbName}", list);
        return list;
    }

    private static void ScanPlaceholder(string s, string token, List<string> sink)
    {
        if (s.Contains(token, StringComparison.OrdinalIgnoreCase))
            sink.Add(token);
    }

    private static AliasTable GetAliasTable(DatabaseType type) => type switch
    {
        DatabaseType.SQLServer => new AliasTable(
            UserIdKeys: Set("user id", "uid"),
            PasswordKeys: Set("password", "pwd"),
            DbNameKeys: Set("initial catalog", "database"),
            KnownOtherKeys: Set("data source", "server", "address", "integrated security", "trusted_connection",
                "encrypt", "trustservercertificate", "application name", "connection timeout", "multipleactiveresultsets")),
        DatabaseType.PostgreSQL => new AliasTable(
            UserIdKeys: Set("username", "user id"),
            PasswordKeys: Set("password"),
            DbNameKeys: Set("database"),
            KnownOtherKeys: Set("host", "server", "port", "ssl mode", "sslmode", "application name",
                "connection idle lifetime", "search path")),
        DatabaseType.MySQL => new AliasTable(
            UserIdKeys: Set("user id", "uid", "username"),
            PasswordKeys: Set("password", "pwd"),
            DbNameKeys: Set("database"),
            KnownOtherKeys: Set("server", "host", "port", "ssl mode", "sslmode", "charset", "default command timeout")),
        DatabaseType.Oracle => new AliasTable(
            UserIdKeys: Set("user id"),
            PasswordKeys: Set("password"),
            DbNameKeys: Set(), // Oracle EZConnect embeds service in Data Source
            KnownOtherKeys: Set("data source", "connection timeout", "pooling")),
        _ => new AliasTable(Set(), Set(), Set(), Set()),
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
