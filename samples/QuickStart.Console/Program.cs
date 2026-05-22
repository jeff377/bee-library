using Bee.Api.Client;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.Messages;

namespace QuickStart.Console;

internal static class Program
{
    private const string DefaultEndpoint = "http://localhost:5050/api";

    public static async Task<int> Main(string[] args)
    {
        var endpoint = ParseEndpoint(args) ?? DefaultEndpoint;

        // ApiKey is required by the framework's default ApiAuthorizationValidator
        // (any non-empty value passes — the demo doesn't validate the key further).
        ApiClientInfo.ApiKey = "quickstart-demo";

        System.Console.WriteLine($"→ endpoint: {endpoint}");
        System.Console.WriteLine();

        try
        {
            await PingAsync(endpoint);
            await EchoAsync(endpoint, "hello from QuickStart.Console");
            return 0;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"✗ failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task PingAsync(string endpoint)
    {
        System.Console.WriteLine("• System.Ping");
        var connector = new SystemApiConnector(endpoint, Guid.Empty);
        await connector.PingAsync();
        System.Console.WriteLine("  status: ok");
        System.Console.WriteLine();
    }

    private static async Task EchoAsync(string endpoint, string message)
    {
        System.Console.WriteLine($"• Echo.Echo (message=\"{message}\")");
        var connector = new FormApiConnector(endpoint, Guid.Empty, "Echo");

        // Echo is declared [ApiAccessControl(Public, Anonymous)] so PayloadFormat.Plain
        // (no encoding / no encryption) is sufficient and avoids the Login-issued
        // RSA hand-shake that Encrypted format would require.
        var result = await connector.ExecuteAsync<EchoResponse>(
            "Echo",
            new EchoRequest { Message = message },
            PayloadFormat.Plain);

        System.Console.WriteLine($"  response : {result.Response}");
        System.Console.WriteLine($"  serverTime: {result.ServerTime:o}");
    }

    private static string? ParseEndpoint(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] is "--endpoint" or "-e")
                return args[i + 1];
        }
        return null;
    }

    /// <summary>
    /// Wire-level request shape for <c>Echo.Echo</c>. The console keeps its own
    /// DTO instead of referencing the server's <c>EchoArgs</c> — that mirrors how
    /// a real third-party client would integrate (it only knows the contract,
    /// not the server-side BO assembly).
    /// </summary>
    private sealed class EchoRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Wire-level response shape for <c>Echo.Echo</c>.
    /// </summary>
    private sealed class EchoResponse
    {
        public string Response { get; set; } = string.Empty;
        public DateTime ServerTime { get; set; }
    }
}
