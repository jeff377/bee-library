using System.Collections.Concurrent;
using Bee.Base;
using Bee.Business.Form;
using Bee.Definition.Customization;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Business
{
    /// <summary>
    /// Resolves the concrete <see cref="FormBusinessObject"/>-derived type for a given
    /// progId by looking up <c>ProgramItem.BusinessObject</c> in <see cref="ProgramSettings"/>,
    /// with an optional tenant customization overlay via <see cref="ICustomizeDefineReader"/>.
    /// </summary>
    /// <remarks>
    /// Resolution behaviour (silent fallback on misconfiguration to avoid taking the
    /// whole system down for a single bad entry):
    /// <list type="bullet">
    ///   <item><description><c>ProgramSettings.xml</c> missing — returns <see cref="FormBusinessObject"/>. Hosts that have not yet shipped a ProgramSettings file behave as if every progId is unregistered.</description></item>
    ///   <item><description>ProgId not registered in <see cref="ProgramSettings"/> — returns <see cref="FormBusinessObject"/>.</description></item>
    ///   <item><description>ProgId registered but <c>BusinessObject</c> empty — returns <see cref="FormBusinessObject"/>.</description></item>
    ///   <item><description><c>BusinessObject</c> set but the type cannot be loaded — returns <see cref="FormBusinessObject"/>.</description></item>
    ///   <item><description><c>BusinessObject</c> set and loaded but the type is not assignable to <see cref="FormBusinessObject"/> — returns <see cref="FormBusinessObject"/>.</description></item>
    ///   <item><description><c>BusinessObject</c> set, loaded, and assignable — returns that type.</description></item>
    /// </list>
    /// When a non-empty customization code is supplied, the customization <see cref="ProgramSettings"/>
    /// is consulted first (per-progId: a customization entry wins, otherwise the base entry applies).
    /// base and cust settings are never merged.
    ///
    /// Resolved types are cached keyed by <c>(customizeId, progId)</c>. When either the base or a
    /// customization <see cref="ProgramSettings"/> instance changes (e.g. after a file-watcher
    /// reload, detected by reference inequality), the type cache is reset on the next call.
    /// </remarks>
    public sealed class ProgramSettingsFormBoTypeResolver : IFormBoTypeResolver
    {
        private readonly IDefineAccess _defineAccess;
        private readonly ICustomizeDefineReader? _customizeReader;
        private readonly ConcurrentDictionary<string, Type> _typeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _resetLock = new();
        private ProgramSettings? _lastSettingsRef;
        private readonly ConcurrentDictionary<string, ProgramSettings?> _lastCustRefs = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsFormBoTypeResolver"/> without customization
        /// support (pure base layer). Backward-compatible convenience overload.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="ProgramSettings"/>.</param>
        public ProgramSettingsFormBoTypeResolver(IDefineAccess defineAccess) : this(defineAccess, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsFormBoTypeResolver"/> with an optional
        /// tenant customization reader.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="ProgramSettings"/>.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the overlay (pure base layer).</param>
        public ProgramSettingsFormBoTypeResolver(IDefineAccess defineAccess, ICustomizeDefineReader? customizeReader)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _customizeReader = customizeReader;
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

            return _typeCache.GetOrAdd(cacheKey, _ => ResolveCore(custSettings, baseSettings, progId));
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

        private static Type ResolveCore(ProgramSettings? custSettings, ProgramSettings? baseSettings, string progId)
        {
            // Which layer wins is decided by CustomizeOverlay — the same class a client runs over
            // the two copies it fetched, so both ends resolve identically.
            var item = CustomizeOverlay.FindProgramItem(custSettings, baseSettings, progId);
            if (item == null || string.IsNullOrWhiteSpace(item.BusinessObject))
                return typeof(FormBusinessObject);

            Type? type;
            try
            {
                // AssemblyLoader.LoadAssembly throws FileNotFoundException when the
                // assembly cannot be located; AssemblyLoader.GetType returns null
                // when the assembly loads but the type is absent. Both cases mean
                // "unresolvable BusinessObject type name" — fall back rather than crash the host.
                type = AssemblyLoader.GetType(item.BusinessObject);
            }
            catch (FileNotFoundException)
            {
                return typeof(FormBusinessObject);
            }
            catch (FileLoadException)
            {
                return typeof(FormBusinessObject);
            }
            catch (BadImageFormatException)
            {
                return typeof(FormBusinessObject);
            }

            if (type == null)
                return typeof(FormBusinessObject);

            if (!typeof(FormBusinessObject).IsAssignableFrom(type))
                return typeof(FormBusinessObject);

            return type;
        }
    }
}
