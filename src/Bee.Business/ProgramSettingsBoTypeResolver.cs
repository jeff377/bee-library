using System.Collections.Concurrent;
using Bee.Base;
using Bee.Business.Form;
using Bee.Definition.Customization;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Extensions.Logging;

namespace Bee.Business
{
    /// <summary>
    /// Resolves the concrete <see cref="BusinessObject"/>-derived type for a given progId by
    /// looking up <c>ProgramItem.BusinessObject</c> in <see cref="ProgramSettings"/>, with an
    /// optional tenant customization overlay via <see cref="ICustomizeDefineReader"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ordinary progIds — silent fallback</b>, so one bad entry does not take the whole system
    /// down. Missing <c>ProgramSettings.xml</c>, unregistered progId, empty <c>BusinessObject</c>,
    /// a type name that will not load, or a type that does not derive from
    /// <see cref="BusinessObject"/> all yield <see cref="FormBusinessObject"/>, which serves
    /// definition-driven CRUD. The cost of a misconfigured <c>Order</c> is that it degrades to
    /// generic CRUD — annoying, not an outage.
    /// </para>
    /// <para>
    /// <b>Reserved progIds — fail fast.</b> The same policy applied to <c>System</c> would produce
    /// a misleading failure: a <see cref="FormBusinessObject"/> has no <c>Login</c>, so the symptom
    /// would be a JSON-RPC "method not found" pointing the diagnosis at the API layer rather than
    /// at the registry. A named type that will not load, or one that does not derive from the
    /// binding's <see cref="ReservedProgIdBinding.ExpectedBaseType"/>, therefore throws.
    /// </para>
    /// <para>
    /// A reserved progId that the registry does not mention at all resolves to the framework's own
    /// type. That is not the rejected "fall back on failure" policy — nothing failed, the entry is
    /// simply absent, and it is exactly what startup self-registration is there to fill in. Keeping
    /// it here rather than mutating the cached <see cref="ProgramSettings"/> is what lets a
    /// read-only deployment start at all: the registration result takes part in resolution whether
    /// or not the file write succeeded, and the process-wide cache instance is never mutated (see
    /// <c>docs/development-constraints.md</c>, Definition Data Immutability After Init). A
    /// <b>customization typo still fails</b>, because that entry exists and simply will not load.
    /// </para>
    /// <para>
    /// When a non-empty customization code is supplied, the customization
    /// <see cref="ProgramSettings"/> is consulted first. The overlay is per progId and then per
    /// property: a customization entry wins for the bindings it names, and every binding it leaves
    /// empty keeps the base value, so a customization that replaces only the business object does
    /// not disturb the repository bound to the same program.
    /// </para>
    /// <para>
    /// Resolved types are cached keyed by <c>(customizeId, progId)</c>. When either the base or a
    /// customization <see cref="ProgramSettings"/> instance changes (e.g. after a file-watcher
    /// reload, detected by reference inequality), the type cache is reset on the next call.
    /// </para>
    /// <para>
    /// <b>A silent degrade is still logged.</b> Falling back keeps the deployment running, but a
    /// misconfigured binding that produces no signal at all is undiagnosable in a multi-tenant
    /// deployment — the symptom is a program quietly behaving generically. The log entry names the
    /// progId, the type that would not resolve, and which layer declared it. Because resolution is
    /// cached, <b>each (customizeId, progId) is logged once</b> and not again until a definition
    /// reload clears the cache; a single line is the expected volume, not a sign that the problem
    /// went away.
    /// </para>
    /// </remarks>
    public sealed class ProgramSettingsBoTypeResolver : IBoTypeResolver
    {
        private readonly IDefineAccess _defineAccess;
        private readonly ICustomizeDefineReader? _customizeReader;
        private readonly ILogger<ProgramSettingsBoTypeResolver>? _logger;
        private readonly ConcurrentDictionary<string, Type> _typeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _resetLock = new();
        private ProgramSettings? _lastSettingsRef;
        private readonly ConcurrentDictionary<string, ProgramSettings?> _lastCustRefs = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsBoTypeResolver"/> without customization
        /// support (pure base layer). Convenience overload.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="ProgramSettings"/>.</param>
        public ProgramSettingsBoTypeResolver(IDefineAccess defineAccess) : this(defineAccess, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsBoTypeResolver"/> with an optional
        /// tenant customization reader.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="ProgramSettings"/>.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the overlay (pure base layer).</param>
        public ProgramSettingsBoTypeResolver(IDefineAccess defineAccess, ICustomizeDefineReader? customizeReader)
            : this(defineAccess, customizeReader, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsBoTypeResolver"/> with an optional
        /// tenant customization reader and an optional logger.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="ProgramSettings"/>.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the overlay (pure base layer).</param>
        /// <param name="logger">Receives the degrade notices described in the type remarks; <c>null</c> silences them.</param>
        public ProgramSettingsBoTypeResolver(
            IDefineAccess defineAccess,
            ICustomizeDefineReader? customizeReader,
            ILogger<ProgramSettingsBoTypeResolver>? logger)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _customizeReader = customizeReader;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Type Resolve(string progId) => Resolve("", progId);

        /// <inheritdoc/>
        public Type Resolve(string customizeId, string progId)
        {
            ArgumentException.ThrowIfNullOrEmpty(progId);

            ProgramSettings? baseSettings;
            try
            {
                baseSettings = _defineAccess.GetProgramSettings();
            }
            catch (FileNotFoundException)
            {
                // Treat a missing ProgramSettings.xml as "no base progIds registered". A
                // customization entry may still resolve below — hosts can adopt the BO-binding
                // feature incrementally without shipping a base ProgramSettings file.
                baseSettings = null;
            }

            // Customization settings — only consulted when a code is present and a reader is wired.
            ProgramSettings? custSettings = null;
            if (!string.IsNullOrEmpty(customizeId) && _customizeReader is not null)
                custSettings = _customizeReader.GetCustomizeProgramSettings(customizeId);

            ResetCacheOnReload(customizeId, baseSettings, custSettings);

            // Composite key keeps each tenant's resolution physically isolated. The empty-customizeId
            // key is just the progId, so the base path is bit-for-bit identical to before. The NUL
            // separator cannot appear in either part, so distinct (customizeId, progId) pairs never collide.
            string cacheKey = string.IsNullOrEmpty(customizeId)
                ? progId
                : customizeId + "\0" + progId;

            return _typeCache.GetOrAdd(cacheKey, _ => ResolveCore(custSettings, baseSettings, customizeId, progId));
        }

        /// <summary>
        /// Resets the whole type cache when the base or the relevant customization
        /// <see cref="ProgramSettings"/> instance changes. Reference equality is enough — the
        /// cache hands back a new instance on file-watcher reload, so a stale reference signals a
        /// stale cache. A full clear on a (rare) reload is simpler and safer than per-key pruning.
        /// </summary>
        private void ResetCacheOnReload(string customizeId, ProgramSettings? baseSettings, ProgramSettings? custSettings)
        {
            bool hasCust = !string.IsNullOrEmpty(customizeId);
            bool baseChanged = !ReferenceEquals(baseSettings, _lastSettingsRef);
            bool custChanged = hasCust
                && (!_lastCustRefs.TryGetValue(customizeId, out var prev) || !ReferenceEquals(prev, custSettings));
            if (!baseChanged && !custChanged)
                return;

            lock (_resetLock)
            {
                baseChanged = !ReferenceEquals(baseSettings, _lastSettingsRef);
                custChanged = hasCust
                    && (!_lastCustRefs.TryGetValue(customizeId, out var prevLocked) || !ReferenceEquals(prevLocked, custSettings));
                if (!baseChanged && !custChanged)
                    return;

                _typeCache.Clear();
                _lastSettingsRef = baseSettings;
                if (hasCust)
                    _lastCustRefs[customizeId] = custSettings;
            }
        }

        private Type ResolveCore(ProgramSettings? custSettings, ProgramSettings? baseSettings, string customizeId, string progId)
        {
            // Which layer wins is decided by CustomizeOverlay — the same class a client runs over
            // the two copies it fetched, so both ends resolve identically.
            var item = CustomizeOverlay.FindProgramItem(custSettings, baseSettings, progId);
            var reserved = ReservedProgIds.Find(progId);

            if (item == null || string.IsNullOrWhiteSpace(item.BusinessObject))
            {
                // A reserved progId the registry does not name yet resolves to the framework's own
                // type — the startup self-registration result, taking effect whether or not it
                // managed to reach the file. Ordinary progIds get the generic CRUD object.
                return reserved?.DefaultType ?? typeof(FormBusinessObject);
            }

            // The merged entry no longer says which layer each binding came from, so ask the
            // customization copy directly. Only the business-object name matters here — the
            // repository name may well have come from the other layer.
            string origin = DescribeOrigin(custSettings, customizeId, progId);

            Type? type;
            try
            {
                // AssemblyLoader.LoadAssembly throws FileNotFoundException when the assembly cannot
                // be located; AssemblyLoader.GetType returns null when the assembly loads but the
                // type is absent. Both mean "unresolvable BusinessObject type name".
                type = AssemblyLoader.GetType(item.BusinessObject);
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                return Unresolvable(reserved, progId, item.BusinessObject, origin, ex);
            }

            if (type == null)
                return Unresolvable(reserved, progId, item.BusinessObject, origin, inner: null);

            var expectedBase = reserved?.ExpectedBaseType ?? typeof(BusinessObject);
            if (!expectedBase.IsAssignableFrom(type))
            {
                if (reserved != null)
                {
                    throw new InvalidOperationException(
                        $"ProgramSettings registers reserved progId '{progId}' as '{item.BusinessObject}', " +
                        $"which does not derive from {expectedBase.FullName}. " +
                        "A reserved progId must resolve to the framework's business object for that axis, or a subclass of it.");
                }

                _logger?.LogError(
                    "ProgramSettings binds progId '{ProgId}' to business object '{TypeName}' ({Origin}), " +
                    "which does not derive from {ExpectedBase}. The program degrades to schema-driven CRUD.",
                    progId, item.BusinessObject, origin, expectedBase.FullName);
                return typeof(FormBusinessObject);
            }

            return type;
        }

        /// <summary>
        /// Names the layer that supplied the business-object binding, for the log entry. Reported as
        /// prose rather than a bare code so an operator reading one line knows whether to look in the
        /// shipped registry or in a specific tenant's override.
        /// </summary>
        private static string DescribeOrigin(ProgramSettings? custSettings, string customizeId, string progId)
        {
            bool fromCustomize = !string.IsNullOrEmpty(customizeId)
                && StringUtilities.IsNotEmpty(custSettings?.Items?.GetOrDefault(progId)?.BusinessObject);
            return fromCustomize
                ? $"declared by customization '{customizeId}'"
                : "declared by the base registry";
        }

        /// <summary>
        /// Decides what an unloadable <c>BusinessObject</c> type name means: a hard failure for a
        /// reserved progId, a logged degrade to generic CRUD for an ordinary one.
        /// </summary>
        private Type Unresolvable(ReservedProgIdBinding? reserved, string progId, string typeName, string origin, Exception? inner)
        {
            if (reserved == null)
            {
                _logger?.LogError(inner,
                    "ProgramSettings binds progId '{ProgId}' to business object '{TypeName}' ({Origin}), " +
                    "which cannot be loaded. The program degrades to schema-driven CRUD.",
                    progId, typeName, origin);
                return typeof(FormBusinessObject);
            }

            throw new InvalidOperationException(
                $"ProgramSettings registers reserved progId '{progId}' as '{typeName}', which cannot be loaded. " +
                "Fix the assembly-qualified type name, or remove the entry to fall back to the framework default.",
                inner);
        }
    }
}
