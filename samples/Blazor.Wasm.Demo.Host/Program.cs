using Bee.Samples.Shared;

namespace Blazor.Wasm.Demo.Host;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Force-load the Wasm client's static web assets manifest in all environments;
        // by default ASP.NET Core only opts into UseStaticWebAssets in Development, which
        // would leave /_framework and index.html unreachable when the demo runs with the
        // built-in Production default (e.g. plain `dotnet run`).
        builder.WebHost.UseStaticWebAssets();

        // Bee backend services (in-process JSON-RPC dispatch) — same wiring as the
        // Blazor Server demo, exposed over HTTP for the Wasm client.
        builder.AddBeeBackend();

        builder.Services.AddControllers();

        var app = builder.Build();
        app.UseBeeBackend();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
