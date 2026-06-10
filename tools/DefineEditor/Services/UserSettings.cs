using System.Text.Json;

namespace Bee.DefineEditor.Services;

/// <summary>
/// Per-user persistent settings for DefineEditor (language choice and the
/// recently opened solutions). Serialised as JSON to an OS-appropriate
/// user-config location so the choices survive across app launches.
/// </summary>
public sealed class UserSettings
{
    /// <summary>
    /// IETF BCP-47 culture name. <c>"en"</c> by default; users who switch via
    /// the View → Language menu get e.g. <c>"zh-TW"</c> written here.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Recently opened DefinePath solution folders, most recent first. Drives
    /// the File → Open Recent menu; capped at <see cref="MaxRecentSolutions"/>.
    /// </summary>
    public List<string> RecentSolutions { get; set; } = new();

    /// <summary>
    /// Whether the Welcome tab opens automatically at startup. Toggled by the
    /// checkbox on the Welcome page itself; the tab stays reachable via
    /// View → Welcome either way.
    /// </summary>
    public bool ShowWelcomeOnStartup { get; set; } = true;

    public const int MaxRecentSolutions = 8;

    /// <summary>
    /// Records <paramref name="path"/> as the most recently opened solution:
    /// moves it to the front (case-insensitive de-dup — macOS and Windows
    /// paths compare case-insensitively) and trims the list to the cap.
    /// </summary>
    public void TouchRecentSolution(string path)
    {
        RecentSolutions.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        RecentSolutions.Insert(0, path);
        if (RecentSolutions.Count > MaxRecentSolutions)
            RecentSolutions.RemoveRange(MaxRecentSolutions, RecentSolutions.Count - MaxRecentSolutions);
    }

    private const string DirectoryName = "Bee.DefineEditor";
    private const string FileName = "settings.json";

    /// <summary>
    /// Resolves the absolute path to this user's settings file. Uses the
    /// platform-conventional location:
    /// <list type="bullet">
    ///   <item><c>~/Library/Application Support/Bee.DefineEditor/settings.json</c> on macOS</item>
    ///   <item><c>%APPDATA%/Bee.DefineEditor/settings.json</c> on Windows</item>
    ///   <item><c>$XDG_CONFIG_HOME/Bee.DefineEditor/settings.json</c> (or <c>~/.config</c>) on Linux</item>
    /// </list>
    /// Uses <see cref="Environment.SpecialFolder.ApplicationData"/> which maps
    /// to the right place on each OS without us hand-rolling per-platform logic.
    /// </summary>
    public static string GetConfigPath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(baseDir, DirectoryName, FileName);
    }

    /// <summary>
    /// Loads the user's settings. Returns a fresh default-valued instance
    /// (English) when the file doesn't exist or is unreadable / malformed —
    /// never throws so app startup can't be blocked by a broken settings file.
    /// </summary>
    public static UserSettings Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path)) return new UserSettings();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new UserSettings();
        }
    }

    /// <summary>
    /// Writes the current settings out. Creates the parent directory if
    /// missing. Silently swallows IO errors — a failed save just means the
    /// next launch will use defaults again; we don't want to bubble a UI
    /// disruption for a non-critical preference write.
    /// </summary>
    public void Save()
    {
        var path = GetConfigPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // best effort
        }
    }
}
