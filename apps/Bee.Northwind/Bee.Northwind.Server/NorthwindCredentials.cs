namespace Bee.Northwind.Server;

/// <summary>
/// Hard-coded credentials accepted by <see cref="NorthwindAuthenticatingSystemBusinessObject"/>.
/// The desktop head surfaces these on its login screen so a fresh visitor knows what to type.
/// </summary>
public static class NorthwindCredentials
{
    /// <summary>The demo user id.</summary>
    public const string UserId = "demo";

    /// <summary>The demo password.</summary>
    public const string Password = "demo";

    /// <summary>The display name surfaced through <c>SessionInfo.UserName</c>.</summary>
    public const string DisplayName = "Demo User";

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
