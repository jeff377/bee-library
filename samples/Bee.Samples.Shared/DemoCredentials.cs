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
}
