using Bee.Business;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bee.Hosting.Registry
{
    /// <summary>
    /// Hosted service that writes the framework's reserved progIds into the type registry at
    /// startup when they are missing, then verifies that each one resolves to a usable business
    /// object. Registered by <c>AddBeeFramework</c> ahead of every other hosted service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The registry is the single source for progId to type bindings, which means the reserved
    /// progIds have to be in it — and equally that a host must not have to hand-author them. This
    /// closes that gap the way COM+ does: the component writes its own registry entries when it is
    /// installed.
    /// </para>
    /// <para>
    /// The work runs per entry rather than per file. Every existing host already ships a
    /// <c>ProgramSettings.xml</c> without a <c>System</c> entry, so a file-exists check would skip
    /// exactly the deployments that need this and leave them unable to log in.
    /// </para>
    /// <para>
    /// It is an <see cref="IHostedService"/> rather than part of <c>AddBeeFramework</c> because the
    /// service provider does not exist yet while services are being registered — building one there
    /// would produce a second container with its own singletons — and because
    /// <c>AddBeeFramework</c> also runs in design-time builds and unit tests, where writing to the
    /// define path would be an unwanted side effect. <see cref="StartAsync"/> rather than
    /// <c>BackgroundService.ExecuteAsync</c>, so it completes before the host accepts the first
    /// request.
    /// </para>
    /// </remarks>
    public sealed class ReservedProgIdRegistrationService : IHostedService
    {
        private readonly IDefineAccess _defineAccess;
        private readonly IBoTypeResolver _resolver;
        private readonly ILogger<ReservedProgIdRegistrationService> _logger;

        /// <summary>
        /// Initializes a new <see cref="ReservedProgIdRegistrationService"/>.
        /// </summary>
        /// <param name="defineAccess">Reads and writes the type registry.</param>
        /// <param name="resolver">The resolver used to verify each reserved progId after registration.</param>
        /// <param name="logger">Logger.</param>
        public ReservedProgIdRegistrationService(
            IDefineAccess defineAccess,
            IBoTypeResolver resolver,
            ILogger<ReservedProgIdRegistrationService> logger)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a reserved progId does not resolve to a usable business object, which stops
        /// the host from starting. Catching this at deployment time is the whole point: left to the
        /// first request it would surface as a JSON-RPC "method not found" and send the diagnosis
        /// to the wrong layer entirely.
        /// </exception>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Register();
            Verify();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Adds any missing reserved entry to the registry and persists the result.
        /// </summary>
        private void Register()
        {
            ProgramSettings registry;
            try
            {
                registry = _defineAccess.GetProgramSettings();
            }
            catch (FileNotFoundException)
            {
                // A host adopting the registry for the first time has no file yet; the reserved
                // entries below are all it needs to start.
                registry = new ProgramSettings();
            }

            var missing = ReservedProgIds.All
                .Where(binding => registry.Items?.Contains(binding.ProgId) != true)
                .ToList();

            if (missing.Count == 0) { return; }

            // Never mutate the instance handed back by the cache: it is shared process-wide, and
            // definitions are immutable after init (docs/development-constraints.md). Build a fresh
            // one, which SaveProgramSettings then persists and whose cache slot it invalidates.
            var updated = new ProgramSettings();
            foreach (var item in registry.Items ?? [])
            {
                // WARNING: every ProgramItem property must be copied here. This rewrites the whole
                // registry file, so anything omitted is silently erased from the host's definition
                // — and only for hosts that happen to be missing a reserved progId, which makes the
                // loss both rare and hard to attribute.
                updated.Items!.Add(new ProgramItem(item.ProgId, item.DisplayName)
                {
                    BusinessObject = item.BusinessObject,
                    Repository = item.Repository,
                });
            }
            foreach (var binding in missing)
            {
                // Repository is left empty on purpose: a reserved progId's business object is not
                // schema-driven CRUD and reaches data through the framework repositories instead.
                updated.Items!.Add(new ProgramItem(binding.ProgId, binding.ProgId) { BusinessObject = binding.DefaultTypeName });
            }

            try
            {
                _defineAccess.SaveProgramSettings(updated);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Registered reserved progId(s) {ProgIds} in ProgramSettings.",
                        string.Join(", ", missing.Select(b => b.ProgId)));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                // A read-only deployment, or another instance winning the same write. Neither stops
                // this process: the resolver already answers a reserved progId that the registry
                // does not name with the framework's own type, so persisting only decides whether
                // the next start has to do this again.
                _logger.LogWarning(ex,
                    "Could not persist reserved progId(s) {ProgIds} to ProgramSettings; resolution uses the framework defaults for this run.",
                    string.Join(", ", missing.Select(b => b.ProgId)));
            }
        }

        /// <summary>
        /// Verifies that every reserved progId resolves to a type derived from its expected base.
        /// </summary>
        private void Verify()
        {
            foreach (var binding in ReservedProgIds.All)
            {
                // Resolution itself throws with the diagnostic detail when the registry names
                // something unloadable or of the wrong family; this re-check covers a custom
                // IBoTypeResolver that answers without those guarantees.
                var type = _resolver.Resolve(binding.ProgId);
                if (!binding.ExpectedBaseType.IsAssignableFrom(type))
                {
                    throw new InvalidOperationException(
                        $"Reserved progId '{binding.ProgId}' resolves to '{type.FullName}', " +
                        $"which does not derive from {binding.ExpectedBaseType.FullName}.");
                }
            }
        }
    }
}
