namespace Bee.Northwind.UI;

/// <summary>
/// Shared client-side defaults referenced by both the UI view models and the platform
/// heads, so the head's <c>Program</c> and the connection screen agree on a single source.
/// </summary>
public static class AppDefaults
{
    /// <summary>
    /// Default JSON-RPC endpoint pre-filled in the connection screen. Matches the
    /// <c>Bee.Northwind.Server</c> default listen URL.
    /// </summary>
    public const string Endpoint = "http://localhost:5100/api";

    /// <summary>API key identifying this client to the backend.</summary>
    public const string ApiKey = "northwind-demo";
}
