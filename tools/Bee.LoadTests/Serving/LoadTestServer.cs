using Bee.Api.Client;
using Bee.LoadTests.Bootstrap;
using Bee.LoadTests.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bee.LoadTests.Serving
{
    /// <summary>
    /// Hosts the JSON-RPC endpoint over HTTP so a Remote run has a server to measure against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because no host in the repository can serve a load test: the sample and demo
    /// servers register SQLite and nothing else, and SQLite is not a load-test target. Rather than
    /// teach one of them to switch providers, the driver serves the same backend it already knows
    /// how to build.
    /// </para>
    /// <para>
    /// It starts the framework through <see cref="LoadTestHost.ConfigureFramework"/>, the same call
    /// a Local run uses. Sharing that step is what lets the difference between a Local and a Remote
    /// measurement be attributed to the transport rather than to two hosts having been set up
    /// differently.
    /// </para>
    /// </remarks>
    public static class LoadTestServer
    {
        /// <summary>
        /// Runs the server until the process is stopped.
        /// </summary>
        /// <param name="options">The run configuration; the database section decides the backend.</param>
        /// <param name="url">The URL to listen on.</param>
        /// <returns>A task that completes when the server shuts down.</returns>
        public static async Task RunAsync(LoadTestOptions options, string url)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(url);

            using var workspace = LoadTestHost.CreateWorkspace(options);

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls(url);

            // Warnings and above only: request logging at Information would put console I/O on the
            // path being measured, which a production host writing elsewhere would not have.
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            LoadTestHost.ConfigureFramework(builder.Services, options, workspace);
            builder.Services.AddControllers();

            var app = builder.Build();

            // The server dispatches in-process from inside the request, so it needs the same
            // service provider hookup a Local run makes.
            ApiClientInfo.LocalServiceProvider = app.Services;
            app.MapControllers();

            Console.WriteLine($"Definitions  : {workspace.DefinePath}");
            if (workspace.DroppedBindings.Count > 0)
            {
                Console.WriteLine($"Dropped      : {string.Join(", ", workspace.DroppedBindings)}");
            }
            Console.WriteLine($"Listening    : {url}");
            Console.WriteLine("Press Ctrl+C to stop.");

            await app.RunAsync().ConfigureAwait(false);
        }
    }
}
