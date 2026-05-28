using System.Collections.Concurrent;
using Bee.Base;
using Bee.Business.Form;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Business
{
    /// <summary>
    /// Resolves the concrete <see cref="FormBusinessObject"/>-derived type for a given
    /// progId by looking up <c>ProgramItem.BusinessObject</c> in <see cref="ProgramSettings"/>.
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
    /// Resolved types are cached for the lifetime of the in-memory
    /// <see cref="ProgramSettings"/> instance. When
    /// <see cref="IDefineAccess.GetProgramSettings"/> returns a different instance
    /// (e.g. after a file-watcher reload), the cache is reset on the next call.
    /// </remarks>
    public sealed class ProgramSettingsFormBoTypeResolver : IFormBoTypeResolver
    {
        private readonly IDefineAccess _defineAccess;
        private readonly ConcurrentDictionary<string, Type> _typeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _resetLock = new();
        private ProgramSettings? _lastSettingsRef;

        /// <summary>
        /// Initializes a new <see cref="ProgramSettingsFormBoTypeResolver"/>.
        /// </summary>
        /// <param name="defineAccess">The define access used to load <see cref="ProgramSettings"/>.</param>
        public ProgramSettingsFormBoTypeResolver(IDefineAccess defineAccess)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        }

        /// <inheritdoc/>
        public Type Resolve(string progId)
        {
            ArgumentException.ThrowIfNullOrEmpty(progId);

            ProgramSettings settings;
            try
            {
                settings = _defineAccess.GetProgramSettings();
            }
            catch (FileNotFoundException)
            {
                // Treat a missing ProgramSettings.xml as "no progIds registered".
                // Hosts can adopt the BO-binding feature incrementally — until they
                // ship a ProgramSettings file the resolver simply mirrors the
                // legacy DefaultFormBoTypeResolver behaviour.
                return typeof(FormBusinessObject);
            }

            // Reset the cache when the underlying ProgramSettings instance changes.
            // Reference equality is enough — ProgramSettingsCache hands back a new
            // instance on file-watcher reload, so a stale reference signals stale cache.
            if (!ReferenceEquals(settings, _lastSettingsRef))
            {
                lock (_resetLock)
                {
                    if (!ReferenceEquals(settings, _lastSettingsRef))
                    {
                        _typeCache.Clear();
                        _lastSettingsRef = settings;
                    }
                }
            }

            return _typeCache.GetOrAdd(progId, key => ResolveCore(settings, key));
        }

        private static Type ResolveCore(ProgramSettings settings, string progId)
        {
            var item = FindItem(settings, progId);
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

        private static ProgramItem? FindItem(ProgramSettings settings, string progId)
        {
            if (settings.Categories == null) return null;

            foreach (var category in settings.Categories)
            {
                var item = category.Items?.GetOrDefault(progId);
                if (item != null) return item;
            }
            return null;
        }
    }
}
