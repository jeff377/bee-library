using Bee.Samples.Shared;
using Bee.Web.Blazor.Server.DependencyInjection;
using Blazor.Server.Demo.Components;

namespace Blazor.Server.Demo;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Bee backend services (in-process JSON-RPC dispatch) — must run before
        // AddBeeBlazor so the Local provider has services to resolve.
        builder.AddBeeBackend();

        // Blazor component services — UseLocalProvider keeps the BeeApiConnectorFactory
        // building connectors against ApiClientInfo.LocalServiceProvider (set in UseBeeBackend).
        builder.Services.AddBeeBlazor(options => options.UseLocalProvider());

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var app = builder.Build();
        app.UseBeeBackend();

        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        app.Run();
    }
}
