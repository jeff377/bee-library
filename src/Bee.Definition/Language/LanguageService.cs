using Bee.Definition.Customization;
using Bee.Definition.Storage;

namespace Bee.Definition.Language
{
    /// <summary>
    /// Default <see cref="ILanguageService"/> implementation backed by
    /// <see cref="IDefineAccess.GetLanguage(string, string)"/> for cache + storage,
    /// with an optional tenant customization overlay via <see cref="ICustomizeDefineReader"/>.
    /// </summary>
    /// <remarks>
    /// Stateless with respect to the current user. Read-through cache lives in
    /// the <c>LanguageResourceCache</c> slot behind <see cref="IDefineAccess"/>;
    /// this service does no caching of its own.
    ///
    /// When a non-empty customization code is supplied, text lookups are overlaid per key: the
    /// customization resource wins when it contains the requested key, otherwise the base value
    /// is used. Enums overlay at whole-enum granularity instead — a customization enum of the same
    /// name replaces the base enum outright (see <c>LookupEnum</c>). base and cust resources are
    /// never merged into a single object, and the base cache is never mutated. An empty
    /// customization code (or no <see cref="ICustomizeDefineReader"/>) short-circuits straight to
    /// the base lookup — bit-for-bit identical to the non-customized path.
    /// </remarks>
    public sealed class LanguageService : ILanguageService
    {
        private readonly IDefineAccess _defineAccess;
        private readonly ICustomizeDefineReader? _customizeReader;

        /// <summary>
        /// Initializes a new <see cref="LanguageService"/> without customization support
        /// (pure base layer). Backward-compatible convenience overload.
        /// </summary>
        /// <param name="defineAccess">The define data access used to load <see cref="LanguageResource"/> entries.</param>
        public LanguageService(IDefineAccess defineAccess) : this(defineAccess, null)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="LanguageService"/> with an optional tenant customization reader.
        /// </summary>
        /// <param name="defineAccess">The define data access used to load <see cref="LanguageResource"/> entries.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the overlay (pure base layer).</param>
        public LanguageService(IDefineAccess defineAccess, ICustomizeDefineReader? customizeReader)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _customizeReader = customizeReader;
        }

        /// <inheritdoc/>
        public string GetLangText(string lang, string fullKey)
        {
            (string @namespace, string subKey) = LanguageKey.Split(fullKey);
            return GetLangText("", lang, @namespace, subKey);
        }

        /// <inheritdoc/>
        public string GetLangText(string lang, string @namespace, string subKey)
            => GetLangText("", lang, @namespace, subKey);

        /// <inheritdoc/>
        public string GetLangText(string customizeId, string lang, string @namespace, string subKey)
        {
            // 1. Primary lookup in the requested language (customization-overlaid).
            if (TryGetLangText(customizeId, lang, @namespace, subKey, out string text))
                return text;

            // 2. Fall back to the system default language (when different).
            string defaultLang = GetDefaultLang();
            if (!string.IsNullOrEmpty(defaultLang)
                && !string.Equals(lang, defaultLang, StringComparison.OrdinalIgnoreCase)
                && TryGetLangText(customizeId, defaultLang, @namespace, subKey, out text))
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
            (string @namespace, string subKey) = LanguageKey.Split(fullKey);
            return TryGetLangText("", lang, @namespace, subKey, out text);
        }

        /// <inheritdoc/>
        public bool TryGetLangText(string lang, string @namespace, string subKey, out string text)
            => TryGetLangText("", lang, @namespace, subKey, out text);

        /// <inheritdoc/>
        public bool TryGetLangText(string customizeId, string lang, string @namespace, string subKey, out string text)
        {
            // This service's job is to obtain the two layers; which one wins is decided by
            // CustomizeOverlay, the same class a client runs over the two copies it fetched.
            // Storage / cache returns LanguageResource (non-nullable signature) but the underlying
            // file may legitimately not exist, so the base value can be null.
            var custResource = GetCustomizeResource(customizeId, lang, @namespace);
            var resource = _defineAccess.GetLanguage(lang, @namespace);
            return CustomizeOverlay.TryGetLangText(custResource, resource, subKey, out text);
        }

        /// <inheritdoc/>
        public LanguageEnum? GetLangEnum(string lang, string fullName)
        {
            (string @namespace, string enumName) = LanguageKey.Split(fullName);
            return GetLangEnum("", lang, @namespace, enumName);
        }

        /// <inheritdoc/>
        public LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName)
            => GetLangEnum("", lang, @namespace, enumName);

        /// <inheritdoc/>
        public LanguageEnum? GetLangEnum(string customizeId, string lang, string @namespace, string enumName)
        {
            if (string.IsNullOrWhiteSpace(@namespace) || string.IsNullOrWhiteSpace(enumName))
                return null;

            // 1. Primary lookup in the requested language (customization-overlaid).
            var hit = LookupEnum(customizeId, lang, @namespace, enumName);
            if (hit != null)
                return hit;

            // 2. Fall back to the system default language (when different).
            string defaultLang = GetDefaultLang();
            if (!string.IsNullOrEmpty(defaultLang)
                && !string.Equals(lang, defaultLang, StringComparison.OrdinalIgnoreCase))
            {
                return LookupEnum(customizeId, defaultLang, @namespace, enumName);
            }

            return null;
        }

        /// <inheritdoc/>
        public string? GetLangEnumText(string lang, string fullName, string code)
            => GetLangEnumText("", lang, fullName, code);

        /// <inheritdoc/>
        public string? GetLangEnumText(string customizeId, string lang, string fullName, string code)
        {
            (string @namespace, string enumName) = LanguageKey.Split(fullName);
            return GetLangEnum(customizeId, lang, @namespace, enumName)?.GetText(code);
        }

        /// <summary>
        /// Resolves an enum for one language, applying the customization overlay at
        /// <b>whole-enum</b> granularity — a customization enum of the same name replaces the base
        /// enum outright.
        /// </summary>
        /// <remarks>
        /// Deliberately coarser than the per-key overlay used for text. A <see cref="LanguageEnum"/>
        /// is an ordered option set, not a bag of independent values: merging entry by entry would
        /// leave the order, and the meaning of an entry the customization omits, ambiguous. A tenant
        /// that customizes an option set therefore owns it whole, and one that does not gets the
        /// base set untouched. Either way the returned instance is the cached one — nothing is
        /// copied and neither layer is mutated.
        /// </remarks>
        private LanguageEnum? LookupEnum(string customizeId, string lang, string @namespace, string enumName)
        {
            var custResource = GetCustomizeResource(customizeId, lang, @namespace);
            var resource = _defineAccess.GetLanguage(lang, @namespace);
            return CustomizeOverlay.GetLangEnum(custResource, resource, enumName);
        }

        /// <summary>
        /// Resolves the customization-override language resource for the given code. Short-circuits
        /// (returns <c>null</c>) when there is no customization code or no reader — the common,
        /// non-customized path never touches the override layer.
        /// </summary>
        private LanguageResource? GetCustomizeResource(string customizeId, string lang, string @namespace)
        {
            if (string.IsNullOrEmpty(customizeId) || _customizeReader is null)
                return null;
            return _customizeReader.GetCustomizeLanguage(customizeId, lang, @namespace);
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
    }
}
