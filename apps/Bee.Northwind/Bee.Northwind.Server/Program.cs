using Bee.Northwind.Server;

const string DevWasmCorsPolicy = "BeeDevWasm";

var builder = WebApplication.CreateBuilder(args);

// Bee backend (in-process JSON-RPC dispatch). AddNorthwindBackend handles PathOptions,
// SQLite registration, AddBeeFramework, and binds the app's SystemBusinessObject subclass.
// demo/demo login runs the framework's own st_user check against the seeded row.
builder.AddNorthwindBackend();

builder.Services.AddControllers();

// Dev-only CORS so the Avalonia WASM head (served by its own dev server on a different
// localhost port) can call this JSON-RPC API cross-origin. Production should serve the WASM
// same-origin from this host and drop the policy entirely (see Bee.Northwind.Browser/README).
builder.Services.AddCors(options =>
    options.AddPolicy(DevWasmCorsPolicy, policy => policy
        .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && uri.Host is "localhost" or "127.0.0.1")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// WARNING: Must run before UseNorthwindBackend so the CORS preflight (OPTIONS) is answered
// before any API access-control middleware can reject the unauthenticated probe.
if (app.Environment.IsDevelopment())
    app.UseCors(DevWasmCorsPolicy);

app.UseNorthwindBackend();
app.MapControllers();
app.Run();
