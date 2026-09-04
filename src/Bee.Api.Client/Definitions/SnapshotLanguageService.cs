using Bee.Definition.Customization;
using Bee.Definition.Language;

namespace Bee.Api.Client.Definitions
{
    /// <summary>
    /// An <see cref="ILanguageService"/> over language resources already fetched from the server —
    /// one base copy and one customization copy per <c>(lang, namespace)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Client-side localization needs a synchronous <see cref="ILanguageService"/>, but fetching is
    /// asynchronous. This service closes that gap by working over a snapshot the caller has already
    /// awaited: decide up front which namespaces are needed, fetch them, then localize without
    /// further I/O.
    /// </para>
    /// <para>
    /// Selection between the two layers goes through <see cref="CustomizeOverlay"/> — the same class
    /// the server uses — so a client and the server resolve the same key to the same text.
    /// </para>
    /// </remarks>
    public sealed class SnapshotLanguageService : ILanguageService
    {
        private readonly IReadOnlyDictionary<string, LanguageLayers> _snapshot;
        private readonly string _defaultLang;

        /// <summary>
        /// Initializes a new <see cref="SnapshotLanguageService"/>.
        /// </summary>
        /// <param name="snapshot">The fetched layers, keyed by <see cref="BuildKey"/>.</param>
        /// <param name="defaultLang">The system default language used for the fall-back hop; empty disables it.</param>
        public SnapshotLanguageService(IReadOnlyDictionary<string, LanguageLayers> snapshot, string defaultLang)
        {
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _defaultLang = defaultLang ?? string.Empty;
        }

        /// <summary>
        /// Builds the snapshot key for a language / namespace pair.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        public static string BuildKey(string lang, string ns) => $"{lang}\0{ns}";

        /// <inheritdoc/>
        public string GetLangText(string lang, string fullKey)
        {
            (string @namespace, string subKey) = LanguageKey.Split(fullKey);
            return GetLangText(lang, @namespace, subKey);
        }

        /// <inheritdoc/>
        public string GetLangText(string lang, string @namespace, string subKey)
        {
            if (TryGetLangText(lang, @namespace, subKey, out string text))
                return text;
            if (UseDefaultLangFallback(lang) && TryGetLangText(_defaultLang, @namespace, subKey, out text))
                return text;
            // Same last resort as the server: surface the key so a missing translation is visible.
            return $"{@namespace}.{subKey}";
        }

        /// <inheritdoc/>
        public bool TryGetLangText(string lang, string fullKey, out string text)
        {
            (string @namespace, string subKey) = LanguageKey.Split(fullKey);
            return TryGetLangText(lang, @namespace, subKey, out text);
        }

        /// <inheritdoc/>
        public bool TryGetLangText(string lang, string @namespace, string subKey, out string text)
        {
            var layers = Find(lang, @namespace);
            return CustomizeOverlay.TryGetLangText(layers.Customize, layers.Base, subKey, out text);
        }

        /// <inheritdoc/>
        public LanguageEnum? GetLangEnum(string lang, string fullName)
        {
            (string @namespace, string enumName) = LanguageKey.Split(fullName);
            return GetLangEnum(lang, @namespace, enumName);
        }

        /// <inheritdoc/>
        public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName)
        {
            if (string.IsNullOrWhiteSpace(@namespace) || string.IsNullOrWhiteSpace(enumName))
                return null;

            var layers = Find(lang, @namespace);
            var hit = CustomizeOverlay.GetLangEnum(layers.Customize, layers.Base, enumName);
            if (hit != null || !UseDefaultLangFallback(lang))
                return hit;

            layers = Find(_defaultLang, @namespace);
            return CustomizeOverlay.GetLangEnum(layers.Customize, layers.Base, enumName);
        }

        /// <inheritdoc/>
        public string? GetLangEnumText(string lang, string fullName, string code)
        {
            (string @namespace, string enumName) = LanguageKey.Split(fullName);
            return GetLangEnum(lang, @namespace, enumName)?.GetText(code);
        }

        private LanguageLayers Find(string lang, string @namespace)
            => _snapshot.TryGetValue(BuildKey(lang, @namespace), out var layers) ? layers : default;

        private bool UseDefaultLangFallback(string lang)
            => !string.IsNullOrEmpty(_defaultLang)
               && !string.Equals(lang, _defaultLang, StringComparison.OrdinalIgnoreCase);
    }
}
