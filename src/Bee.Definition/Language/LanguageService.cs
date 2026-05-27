using Bee.Definition.Storage;

namespace Bee.Definition.Language
{
    /// <summary>
    /// Default <see cref="ILanguageService"/> implementation backed by
    /// <see cref="IDefineAccess.GetLanguage(string, string)"/> for cache + storage.
    /// </summary>
    /// <remarks>
    /// Stateless with respect to the current user. Read-through cache lives in
    /// the <c>LanguageResourceCache</c> slot behind <see cref="IDefineAccess"/>;
    /// this service does no caching of its own.
    /// </remarks>
    public sealed class LanguageService : ILanguageService
    {
        private readonly IDefineAccess _defineAccess;

        /// <summary>
        /// Initializes a new <see cref="LanguageService"/>.
        /// </summary>
        /// <param name="defineAccess">The define data access used to load <see cref="LanguageResource"/> entries.</param>
        public LanguageService(IDefineAccess defineAccess)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        }

        /// <inheritdoc/>
        public string GetLangText(string lang, string fullKey)
        {
            SplitFullKey(fullKey, out string @namespace, out string subKey);
            return GetLangText(lang, @namespace, subKey);
        }

        /// <inheritdoc/>
        public string GetLangText(string lang, string @namespace, string subKey)
        {
            // 1. Primary lookup in the requested language.
            if (TryGetLangText(lang, @namespace, subKey, out string text))
                return text;

            // 2. Fall back to the system default language (when different).
            string defaultLang = GetDefaultLang();
            if (!string.IsNullOrEmpty(defaultLang)
                && !string.Equals(lang, defaultLang, StringComparison.OrdinalIgnoreCase)
                && TryGetLangText(defaultLang, @namespace, subKey, out text))
            {
                return text;
            }

            // 3. Final fall-back: return the full key string so the missing
            //    translation is visible in the UI (developers can spot it).
            return $"{@namespace}.{subKey}";
        }

        /// <inheritdoc/>
        public bool TryGetLangText(string lang, string fullKey, out string text)
        {
            SplitFullKey(fullKey, out string @namespace, out string subKey);
            return TryGetLangText(lang, @namespace, subKey, out text);
        }

        /// <inheritdoc/>
        public bool TryGetLangText(string lang, string @namespace, string subKey, out string text)
        {
            // Storage / cache returns LanguageResource (non-nullable signature) but
            // the underlying file may legitimately not exist. The actual value can
            // be null and we must guard.
            var resource = _defineAccess.GetLanguage(lang, @namespace);
            if (resource is null || !resource.Items.Contains(subKey))
            {
                text = string.Empty;
                return false;
            }
            text = resource.Items[subKey].Value;
            return true;
        }

        /// <inheritdoc/>
        public LanguageEnum? GetLangEnum(string lang, string fullName)
        {
            SplitFullKey(fullName, out string @namespace, out string enumName);
            return GetLangEnum(lang, @namespace, enumName);
        }

        /// <inheritdoc/>
        public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName)
        {
            if (string.IsNullOrWhiteSpace(@namespace) || string.IsNullOrWhiteSpace(enumName))
                return null;

            // 1. Primary lookup in the requested language.
            var hit = LookupEnum(lang, @namespace, enumName);
            if (hit != null)
                return hit;

            // 2. Fall back to the system default language (when different).
            string defaultLang = GetDefaultLang();
            if (!string.IsNullOrEmpty(defaultLang)
                && !string.Equals(lang, defaultLang, StringComparison.OrdinalIgnoreCase))
            {
                return LookupEnum(defaultLang, @namespace, enumName);
            }

            return null;
        }

        /// <inheritdoc/>
        public string? GetLangEnumText(string lang, string fullName, string code)
        {
            var langEnum = GetLangEnum(lang, fullName);
            return langEnum?.GetText(code);
        }

        private LanguageEnum? LookupEnum(string lang, string @namespace, string enumName)
        {
            var resource = _defineAccess.GetLanguage(lang, @namespace);
            return resource?.GetEnum(enumName);
        }

        /// <summary>
        /// Reads the system default language from <see cref="Settings.CommonConfiguration.DefaultLang"/>.
        /// </summary>
        /// <returns>The default lang, or an empty string when settings are unavailable.</returns>
        private string GetDefaultLang()
        {
            var settings = _defineAccess.GetSystemSettings();
            return settings?.CommonConfiguration?.DefaultLang ?? string.Empty;
        }

        /// <summary>
        /// Splits the full key on the first <c>.</c> into namespace + sub-key.
        /// Inputs without a <c>.</c> are treated as namespace-only with an empty sub-key.
        /// </summary>
        private static void SplitFullKey(string fullKey, out string @namespace, out string subKey)
        {
            ArgumentNullException.ThrowIfNull(fullKey);
            int dot = fullKey.IndexOf('.');
            if (dot < 0)
            {
                @namespace = fullKey;
                subKey = string.Empty;
                return;
            }
            @namespace = fullKey.Substring(0, dot);
            subKey = fullKey.Substring(dot + 1);
        }
    }
}
