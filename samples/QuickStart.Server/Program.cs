using Bee.Business;
using Bee.Samples.Shared;
using QuickStart.Server.BusinessObjects;

namespace QuickStart.Server;

internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Bee backend (in-process JSON-RPC dispatch) — shared with the Blazor demos.
        // DemoBackend handles PathOptions, SQLite registration, AddBeeFramework, and
        // swaps in DemoAuthenticatingSystemBusinessObject so demo/demo Login works
        // without seeding st_user.
        builder.AddBeeBackend();

        // Override the default resolver so progId "Echo" dispatches to the sample's
        // EchoBusinessObject. Order matters: this AddSingleton runs after
        // AddBeeFramework's DefaultFormBoTypeResolver, so the last registration wins
        // when the container resolves IFormBoTypeResolver.
        builder.Services.AddSingleton<IFormBoTypeResolver, QuickStartFormBoTypeResolver>();

        // CORS for the Web.Js.Demo sample (cross-origin JS calling the JSON-RPC endpoint).
        // Demo-only permissive policy — production hosts must restrict origins explicitly.
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
        });

        builder.Services.AddControllers();

        var app = builder.Build();
        app.UseBeeBackend();
        app.UseCors();
        app.MapControllers();
        app.Run();
    }
}
