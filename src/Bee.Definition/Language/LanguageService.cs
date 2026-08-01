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
    /// When a non-empty customization code is supplied, the lookup is overlaid at the finest
    /// granularity the artifact has: per text key, and per <see cref="LanguageEnum"/> entry. The
    /// customization value wins where it exists, the base value applies everywhere else, and a
    /// key / entry only the customization declares is added. The two resources are looked up
    /// independently and the base cache is never mutated — an overlaid enum is returned as a fresh
    /// instance rather than by writing into either layer. An empty customization code (or no
    /// <see cref="ICustomizeDefineReader"/>) short-circuits straight to the base lookup —
    /// bit-for-bit identical to the non-customized path.
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
            SplitFullKey(fullKey, out string @namespace, out string subKey);
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
            SplitFullKey(fullKey, out string @namespace, out string subKey);
            return TryGetLangText("", lang, @namespace, subKey, out text);
        }

        /// <inheritdoc/>
        public bool TryGetLangText(string lang, string @namespace, string subKey, out string text)
            => TryGetLangText("", lang, @namespace, subKey, out text);

        /// <inheritdoc/>
        public bool TryGetLangText(string customizeId, string lang, string @namespace, string subKey, out string text)
        {
            // Customization overlay: a cust resource that contains the key wins. base and cust are
            // looked up independently and never merged — the base cache stays untouched.
            if (TryGetCustomizeResource(customizeId, lang, @namespace, out var custResource)
                && custResource!.Items.Contains(subKey))
            {
                text = custResource.Items[subKey].Value;
                return true;
            }

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
            SplitFullKey(fullName, out string @namespace, out string enumName);
            return GetLangEnum(customizeId, lang, @namespace, enumName)?.GetText(code);
        }

        private LanguageEnum? LookupEnum(string customizeId, string lang, string @namespace, string enumName)
        {
            LanguageEnum? custEnum = null;
            if (TryGetCustomizeResource(customizeId, lang, @namespace, out var custResource))
                custEnum = custResource!.GetEnum(enumName);

            var baseEnum = _defineAccess.GetLanguage(lang, @namespace)?.GetEnum(enumName);

            // The common paths hand back the cached instance untouched: no customization at all,
            // or only one of the two layers declares this enum.
            if (custEnum is null)
                return baseEnum;
            if (baseEnum is null)
                return custEnum;

            return MergeEnum(baseEnum, custEnum);
        }

        /// <summary>
        /// Overlays a customization enum onto its base counterpart at <b>entry</b> granularity,
        /// mirroring how text keys overlay: an entry the customization declares wins, every other
        /// entry keeps its base text, and an entry only the customization declares is appended.
        /// </summary>
        /// <param name="baseEnum">The base-layer enum. Never mutated.</param>
        /// <param name="custEnum">The customization-layer enum. Never mutated.</param>
        /// <returns>A new <see cref="LanguageEnum"/> owned by nobody, safe for the caller to read.</returns>
        /// <remarks>
        /// <para>
        /// Order is the base document order, with customization-only entries appended in their own
        /// order — so a customization that renames two options does not reshuffle the dropdown.
        /// </para>
        /// <para>
        /// A customization <b>cannot remove</b> a base entry: the file expresses "override or add",
        /// with no way to say "drop this code". Removal would change which values the field accepts
        /// and is deliberately not expressible here.
        /// </para>
        /// <para>
        /// Both inputs are process-shared cache instances, so the merge allocates a fresh result
        /// rather than writing into either one. The allocation happens only when a tenant customizes
        /// an enum the base layer also declares; every other path returns a cached instance directly.
        /// </para>
        /// </remarks>
        private static LanguageEnum MergeEnum(LanguageEnum baseEnum, LanguageEnum custEnum)
        {
            var merged = new LanguageEnum { Name = baseEnum.Name };

            foreach (var entry in baseEnum.Entries)
            {
                string text = custEnum.Entries.Contains(entry.Code)
                    ? custEnum.Entries[entry.Code].Text
                    : entry.Text;
                merged.Entries.Add(entry.Code, text);
            }

            foreach (var entry in custEnum.Entries)
            {
                if (!baseEnum.Entries.Contains(entry.Code))
                    merged.Entries.Add(entry.Code, entry.Text);
            }

            return merged;
        }

        /// <summary>
        /// Resolves the customization-override language resource for the given code. Short-circuits
        /// (returns <c>false</c>) when there is no customization code or no reader — the common,
        /// non-customized path never touches the override layer.
        /// </summary>
        private bool TryGetCustomizeResource(string customizeId, string lang, string @namespace, out LanguageResource? resource)
        {
            resource = null;
            if (string.IsNullOrEmpty(customizeId) || _customizeReader is null)
                return false;
            resource = _customizeReader.GetCustomizeLanguage(customizeId, lang, @namespace);
            return resource is not null;
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
