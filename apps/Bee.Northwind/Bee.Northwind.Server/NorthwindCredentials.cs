namespace Bee.Northwind.Server;

/// <summary>
/// The demo account the seeder writes into <c>st_user</c>, and the single company the session
/// enters at login. The desktop head surfaces the credentials on its login screen so a fresh
/// visitor knows what to type.
/// </summary>
/// <remarks>
/// These are seed values, not a credential check: sign-in runs the framework's own <c>st_user</c>
/// authentication, so the password below is only ever hashed on the way in and compared against
/// the stored hash on the way back.
/// </remarks>
public static class NorthwindCredentials
{
    /// <summary>The demo user id.</summary>
    public const string UserId = "demo";

    /// <summary>The demo password.</summary>
    public const string Password = "demo";

    /// <summary>The display name surfaced through <c>SessionInfo.UserName</c>.</summary>
    public const string DisplayName = "Demo User";

    /// <summary>
    /// The demo user's IANA time zone, seeded into <c>st_user.time_zone</c>. The session takes its
    /// zone from the user's row, which is what every user-facing date is then resolved against.
    /// </summary>
    public const string TimeZone = "Asia/Taipei";

    /// <summary>The demo user's culture, seeded into <c>st_user.culture</c>.</summary>
    public const string Culture = "en-US";

    /// <summary>
    /// The single demo company the session auto-enters at login. Company-scoped forms
    /// (<c>CategoryId="company"</c>) resolve their database through this id.
    /// </summary>
    public const string CompanyId = "NORTHWIND";

    /// <summary>The demo company display name.</summary>
    public const string CompanyName = "Northwind Traders";

    /// <summary>
    /// The logical <c>DatabaseSettings</c> id backing the demo company — the
    /// <c>CompanyInfo.CompanyDatabaseId</c> the router resolves company scope to.
    /// </summary>
    public const string CompanyDatabaseId = "company";

    /// <summary>
    /// Hard-coded Base64 AES-CBC-HMAC combined key (64 bytes) used by the demo when
    /// <c>BEE_MASTER_KEY</c> is not set in the environment.
    /// </summary>
    /// <remarks>
    /// Demo-only: a fixed value lets a fresh clone <c>dotnet run</c> with zero setup, and
    /// keeps rows encrypted on one run decryptable on the next. Production hosts MUST inject
    /// a real <c>BEE_MASTER_KEY</c> via the deployment mechanism before
    /// <see cref="NorthwindBackend.AddNorthwindBackend"/> runs.
    /// </remarks>
    public const string DemoMasterKey =
        "epzayQV2UPmasMTfmO91cY25/7J35oNUvkNahhYZCl7qEXOdwluR2e41BJ5WIT7c5zVkSFFaDxrXzMiIUe2Dxw==";
}
