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
    /// <b>A declared binding that will not resolve throws</b>, for every progId. A type name that
    /// will not load, or a type that does not derive from the expected base, is a configuration
    /// error with no harmless reading: the deployment says "this program <i>is</i> that type", and
    /// the type is not there. Falling back to <see cref="FormBusinessObject"/> would buy only the
    /// appearance of a running system — that object accepts any progId in its constructor, so it
    /// constructs happily and the failure surfaces later, as a missing method or as generic
    /// behaviour where custom logic was expected, pointing the diagnosis at the API layer rather
    /// than at the registry. This is the same policy the repository axis has always had.
    /// </para>
    /// <para>
    /// <b>Reserved progIds additionally constrain the base type.</b> <c>System</c> and
    /// <c>AuditLog</c> must resolve to the framework's business object for that axis, or a subclass
    /// of it (<see cref="ReservedProgIdBinding.ExpectedBaseType"/>); an ordinary progId only has to
    /// derive from <see cref="BusinessObject"/>.
    /// </para>
    /// <para>
    /// <b>An absent entry is not a failure.</b> A progId the registry does not mention, or one whose
    /// <c>BusinessObject</c> is empty, resolves to the framework default —
    /// <see cref="FormBusinessObject"/> for an ordinary progId, and the framework's own type for a
    /// reserved one. Nothing failed there: the entry is simply absent, which for a reserved progId
    /// is exactly what startup self-registration fills in. Keeping that here rather than mutating
    /// the cached <see cref="ProgramSettings"/> is what lets a read-only deployment start at all —
    /// the registration result takes part in resolution whether or not the file write succeeded,
    /// and the process-wide cache instance is never mutated (see
    /// <c>docs/development-constraints.md</c>, Definition Data Immutability After Init).
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
    /// Only successes are cached — a failing resolution leaves no entry, so a broken binding throws
    /// on every call rather than passing quietly from the second one on.
    /// </para>
    /// <para>
    /// Every exception names the progId, the type that would not resolve, and which layer declared
    /// it — a multi-tenant deployment needs to know whether to look in the shipped registry or in
    /// one tenant's override.
    /// </para>
    /// </remarks>
    public sealed class ProgramSettingsBoTypeResolver : IBoTypeResolver
    {
        private readonly IDefineAccess _defineAccess;
        private readonly ICustomizeDefineReader? _customizeReader;
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
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _customizeReader = customizeReader;
        }

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsBoTypeResolver"/> with an optional
        /// tenant customization reader. The logger is no longer used and is ignored.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="ProgramSettings"/>.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the overlay (pure base layer).</param>
        /// <param name="logger">
        /// Ignored. It received the degrade notices that existed while an unresolvable ordinary
        /// binding fell back to <see cref="FormBusinessObject"/>; every such binding now throws, so
        /// there is nothing left to report. The overload is kept so existing call sites still
        /// compile and bind.
        /// </param>
        public ProgramSettingsBoTypeResolver(
            IDefineAccess defineAccess,
            ICustomizeDefineReader? customizeReader,
            ILogger<ProgramSettingsBoTypeResolver>? logger)
            : this(defineAccess, customizeReader)
        {
            _ = logger;
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

        private static Type ResolveCore(ProgramSettings? custSettings, ProgramSettings? baseSettings, string customizeId, string progId)
        {
            // Which layer wins is decided by CustomizeOverlay — the same class a client runs over
            // the two copies it fetched, so both ends resolve identically.
            var item = CustomizeOverlay.FindProgramItem(custSettings, baseSettings, progId);
            var reserved = ReservedProgIds.Find(progId);

            if (item == null || string.IsNullOrWhiteSpace(item.BusinessObject))
            {
                // Nothing was declared, so nothing failed. A reserved progId the registry does not
                // name yet resolves to the framework's own type — the startup self-registration
                // result, taking effect whether or not it managed to reach the file. Ordinary
                // progIds get the generic CRUD object. This is the one path that does not throw:
                // below, a binding that names a type is held to that name.
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
                throw Unloadable(reserved, progId, item.BusinessObject, origin, ex);
            }

            if (type == null)
                throw Unloadable(reserved, progId, item.BusinessObject, origin, inner: null);

            var expectedBase = reserved?.ExpectedBaseType ?? typeof(BusinessObject);
            if (!expectedBase.IsAssignableFrom(type))
                throw NotDerived(reserved, progId, item.BusinessObject, origin, expectedBase);

            return type;
        }

        /// <summary>
        /// Names the layer that supplied the business-object binding, for the exception message.
        /// Reported as prose rather than a bare code so an operator reading one line knows whether to
        /// look in the shipped registry or in a specific tenant's override.
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
        /// Builds the failure for a <c>BusinessObject</c> type name that will not load. Reserved and
        /// ordinary progIds fail alike; only the wording distinguishes them, because which one it is
        /// changes where the operator looks.
        /// </summary>
        private static InvalidOperationException Unloadable(
            ReservedProgIdBinding? reserved, string progId, string typeName, string origin, Exception? inner)
        {
            return new InvalidOperationException(
                $"ProgramSettings registers {Subject(reserved, progId)} as '{typeName}' ({origin}), which cannot be loaded. " +
                "Fix the assembly-qualified type name, or clear the binding to fall back to the framework default.",
                inner);
        }

        /// <summary>
        /// Builds the failure for a <c>BusinessObject</c> type that loads but does not derive from the
        /// base the progId requires.
        /// </summary>
        private static InvalidOperationException NotDerived(
            ReservedProgIdBinding? reserved, string progId, string typeName, string origin, Type expectedBase)
        {
            string remedy = reserved != null
                ? "A reserved progId must resolve to the framework's business object for that axis, or a subclass of it."
                : "Bind the progId to a business object, or clear the binding to fall back to schema-driven CRUD.";

            return new InvalidOperationException(
                $"ProgramSettings registers {Subject(reserved, progId)} as '{typeName}' ({origin}), " +
                $"which does not derive from {expectedBase.FullName}. {remedy}");
        }

        /// <summary>
        /// Names the progId in an exception message, marking it as reserved when it is one.
        /// </summary>
        private static string Subject(ReservedProgIdBinding? reserved, string progId)
            => reserved != null ? $"reserved progId '{progId}'" : $"progId '{progId}'";
    }
}
