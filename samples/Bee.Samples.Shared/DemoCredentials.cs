namespace Bee.Samples.Shared;

/// <summary>
/// Hard-coded credentials accepted by <see cref="DemoAuthenticatingSystemBusinessObject"/>.
/// The Blazor demos surface these in their landing page so a fresh visitor knows what to
/// type in the <c>BeeLoginPanel</c>.
/// </summary>
public static class DemoCredentials
{
    /// <summary>The demo user id (shown in the Blazor login panel hint).</summary>
    public const string UserId = "demo";

    /// <summary>The demo password (shown in the Blazor login panel hint).</summary>
    public const string Password = "demo";

    /// <summary>The display name surfaced through <c>SessionInfo.UserName</c>.</summary>
    public const string DisplayName = "Demo User";

    /// <summary>
    /// Hard-coded Base64 AES-CBC-HMAC combined key (64 bytes) used by the bundled
    /// demos when <c>BEE_MASTER_KEY</c> is not set in the environment.
    /// </summary>
    /// <remarks>
    /// Demo-only: keeping a fixed value here means a fresh clone can <c>dotnet run</c>
    /// with zero setup and that <c>quickstart.db</c> rows encrypted on one run still
    /// decrypt on the next. Production hosts MUST inject a real
    /// <c>BEE_MASTER_KEY</c> via the deployment mechanism (K8s Secret, env file,
    /// Vault, etc.) before <see cref="DemoBackend.AddBeeBackend"/> runs — see
    /// <c>samples/README.md</c>.
    /// </remarks>
    public const string DemoMasterKey =
        "epzayQV2UPmasMTfmO91cY25/7J35oNUvkNahhYZCl7qEXOdwluR2e41BJ5WIT7c5zVkSFFaDxrXzMiIUe2Dxw==";
}
