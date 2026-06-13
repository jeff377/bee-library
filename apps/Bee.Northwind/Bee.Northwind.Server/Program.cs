using Bee.Northwind.Server;

var builder = WebApplication.CreateBuilder(args);

// Bee backend (in-process JSON-RPC dispatch). AddNorthwindBackend handles PathOptions,
// SQLite registration, AddBeeFramework, and swaps in the demo authenticating factory so
// demo/demo login works without seeding st_user.
builder.AddNorthwindBackend();

builder.Services.AddControllers();

var app = builder.Build();
app.UseNorthwindBackend();
app.MapControllers();
app.Run();
