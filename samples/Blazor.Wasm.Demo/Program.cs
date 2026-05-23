using Bee.Api.Client;
using Bee.Web.Blazor.Wasm.DependencyInjection;
using Blazor.Wasm.Demo;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ApiKey is required by the framework's default ApiAuthorizationValidator
// (any non-empty value passes — the demo doesn't validate the key further).
ApiClientInfo.ApiKey = "wasm-demo";

// Bee Blazor Wasm components — always Remote; endpoint resolves to the page's own host
// (the Wasm app is served by Blazor.Wasm.Demo.Host on the same origin).
var endpoint = $"{builder.HostEnvironment.BaseAddress}api";
builder.Services.AddBeeBlazor(options => options.UseRemoteProvider(endpoint));

await builder.Build().RunAsync();
