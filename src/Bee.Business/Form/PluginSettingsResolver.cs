using System.Collections.Concurrent;
using Bee.Base;
using Bee.Definition.Customization;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Business.Form
{
    /// <summary>
    /// Default <see cref="IFormPluginResolver"/>: reads the plugin chain from
    /// <see cref="PluginSettings"/>, base layer plus optional tenant customization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every failure throws</b>, as on the business-object and repository axes: a declared name
    /// that will not load is a configuration error, not something to work around. A plugin is
    /// something the author added on purpose, and skipping it would run the pipeline as though the
    /// customization were not there. Silently omitting a credit check is worse than refusing to save.
    /// </para>
    /// <para>
    /// Chains are cached by <c>(customizationCode, progId)</c>, which is also what makes the
    /// override-detection reflection a one-off. When either layer's <see cref="PluginSettings"/>
    /// instance changes — a file-watcher reload, detected by reference inequality — the cache is
    /// reset on the next call.
    /// </para>
    /// </remarks>
    public sealed class PluginSettingsResolver : IFormPluginResolver
    {
        private readonly IDefineAccess _defineAccess;
        private readonly ICustomizeDefineReader? _customizeReader;
        private readonly ConcurrentDictionary<string, FormPluginChain> _chainCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _resetLock = new();
        private PluginSettings? _lastSettingsRef;
        private readonly ConcurrentDictionary<string, PluginSettings?> _lastCustRefs = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new <see cref="PluginSettingsResolver"/> without customization support.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="PluginSettings"/>.</param>
        public PluginSettingsResolver(IDefineAccess defineAccess) : this(defineAccess, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="PluginSettingsResolver"/>.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="PluginSettings"/>.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the overlay.</param>
        public PluginSettingsResolver(IDefineAccess defineAccess, ICustomizeDefineReader? customizeReader)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _customizeReader = customizeReader;
        }

        /// <inheritdoc/>
        public FormPluginChain Resolve(string customizeId, string progId)
        {
            ArgumentException.ThrowIfNullOrEmpty(progId);

            PluginSettings? baseSettings;
            try
            {
                baseSettings = _defineAccess.GetPluginSettings();
            }
            catch (FileNotFoundException)
            {
                // No base definition is the normal state — most deployments bind no plugins at all.
                // A customization may still declare some, so resolution continues.
                baseSettings = null;
            }

            PluginSettings? custSettings = null;
            if (!string.IsNullOrEmpty(customizeId) && _customizeReader is not null)
                custSettings = _customizeReader.GetCustomizePluginSettings(customizeId);

            ResetCacheOnReload(customizeId, baseSettings, custSettings);

            // The NUL separator cannot occur in either part, so distinct pairs never collide; the
            // empty-code key is just the progId, keeping the base path identical to a host with no
            // customization configured.
            string cacheKey = string.IsNullOrEmpty(customizeId)
                ? progId
                : customizeId + "\0" + progId;

            return _chainCache.GetOrAdd(cacheKey, _ => BuildChain(custSettings, baseSettings, progId));
        }

        /// <summary>
        /// Resets the whole chain cache when the base or the relevant customization
        /// <see cref="PluginSettings"/> instance changes. Reference equality is enough — the cache
        /// hands back a new instance on reload, so a stale reference signals a stale chain.
        /// </summary>
        private void ResetCacheOnReload(string customizeId, PluginSettings? baseSettings, PluginSettings? custSettings)
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

                _chainCache.Clear();
                _lastSettingsRef = baseSettings;
                if (hasCust)
                    _lastCustRefs[customizeId] = custSettings;
            }
        }

        private static FormPluginChain BuildChain(PluginSettings? custSettings, PluginSettings? baseSettings, string progId)
        {
            // How the two layers combine is decided by CustomizeOverlay — plugins concatenate,
            // base first, which is the one granularity there that adds rather than chooses.
            var typeNames = CustomizeOverlay.GetPluginTypes(custSettings, baseSettings, progId);
            if (typeNames.Count == 0) { return FormPluginChain.Empty; }

            var types = new List<Type>(typeNames.Count);
            foreach (var typeName in typeNames)
                types.Add(LoadPluginType(progId, typeName));
            return FormPluginChain.Create(types);
        }

        private static Type LoadPluginType(string progId, string typeName)
        {
            Type? type;
            try
            {
                // AssemblyLoader.LoadAssembly throws when the assembly cannot be located;
                // AssemblyLoader.GetType returns null when it loads but the type is absent.
                type = AssemblyLoader.GetType(typeName);
            }
            catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
            {
                throw Unloadable(progId, typeName, ex);
            }

            if (type == null)
                throw Unloadable(progId, typeName, inner: null);

            if (!typeof(FormBusinessPlugin).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    $"PluginSettings binds progId '{progId}' to plugin '{typeName}', which does not derive from " +
                    $"{typeof(FormBusinessPlugin).FullName}.");
            }

            return type;
        }

        private static InvalidOperationException Unloadable(string progId, string typeName, Exception? inner)
            => new(
                $"PluginSettings binds progId '{progId}' to plugin '{typeName}', which cannot be loaded. " +
                "Fix the assembly-qualified type name, or remove the binding.",
                inner);
    }
}
