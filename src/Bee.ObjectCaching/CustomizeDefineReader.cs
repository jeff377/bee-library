using Bee.Definition;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Default <see cref="ICustomizeDefineReader"/>: reads the three customizable types from the
    /// per-customization-code override containers supplied by an
    /// <see cref="ICacheContainerProvider"/>. Hits return the cached read-only instance; a missing
    /// override file returns <c>null</c> without falling back to the base layer.
    /// </summary>
    public sealed class CustomizeDefineReader : ICustomizeDefineReader
    {
        private readonly ICacheContainerProvider _provider;
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new <see cref="CustomizeDefineReader"/>.
        /// </summary>
        /// <param name="provider">Supplies the per-customization-code override cache containers.</param>
        /// <param name="paths">The host path options; <see cref="PathOptions.CustomizePath"/> gates whether customization is enabled at all.</param>
        public CustomizeDefineReader(ICacheContainerProvider provider, PathOptions paths)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <inheritdoc/>
        public LanguageResource? GetCustomizeLanguage(string custCode, string lang, string ns)
        {
            if (!IsCustomizeEnabled(custCode))
                return null;
            return _provider.For(custCode).LanguageResource.Get(lang, ns);
        }

        /// <inheritdoc/>
        public ProgramSettings? GetCustomizeProgramSettings(string custCode)
        {
            if (!IsCustomizeEnabled(custCode))
                return null;

            // ProgramSettingsCache reads the file directly and throws when it is absent (a missing
            // base ProgramSettings.xml is a fatal misconfiguration). For the override layer a
            // missing file is normal, so probe existence first and skip the cache entirely when the
            // tenant supplies no override — this avoids both exception-driven control flow and any
            // change to the shared cache class.
            var custPaths = new CustomizeOnlyPathOptions(_paths.CustomizePath, custCode);
            if (!File.Exists(custPaths.GetProgramSettingsFilePath()))
                return null;
            return _provider.For(custCode).ProgramSettings.Get();
        }

        /// <inheritdoc/>
        public FormLayout? GetCustomizeFormLayout(string custCode, string layoutId)
        {
            if (!IsCustomizeEnabled(custCode))
                return null;
            return _provider.For(custCode).FormLayout.Get(layoutId);
        }

        /// <summary>
        /// Second line of defense: the customization layer is reachable only when a customization
        /// code is present and a customization root is configured. Consumers short-circuit on an
        /// empty code before ever calling this reader, so in normal operation this guard is a no-op.
        /// </summary>
        private bool IsCustomizeEnabled(string custCode)
            => !string.IsNullOrEmpty(custCode) && !string.IsNullOrEmpty(_paths.CustomizePath);
    }
}
