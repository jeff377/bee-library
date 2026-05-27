namespace Bee.Definition.Language
{
    /// <summary>
    /// Service for resolving localized text from <see cref="LanguageResource"/> data.
    /// Caches at the (lang, namespace) granularity through the underlying
    /// <see cref="Bee.Definition.Storage.IDefineAccess.GetLanguage(string, string)"/>
    /// cache slot — invalidating a single namespace in one language does not
    /// affect other namespaces or other languages.
    /// </summary>
    /// <remarks>
    /// The service is stateless with respect to the current user — the caller
    /// passes <c>lang</c> explicitly. BO base classes provide a convenience wrapper
    /// that reads <see cref="Identity.SessionInfo.Culture"/> for the current call.
    ///
    /// Fall-back chain when a key cannot be resolved in the requested language:
    /// <list type="number">
    /// <item><description>Look up <c>(lang, namespace).subKey</c>.</description></item>
    /// <item><description>If miss and <c>lang</c> ≠ <c>DefaultLang</c>,
    /// retry against <c>(DefaultLang, namespace).subKey</c>
    /// (<c>DefaultLang</c> is read from <see cref="Settings.CommonConfiguration.DefaultLang"/>).</description></item>
    /// <item><description>If still miss, return the full key string as-is so the
    /// missing translation is visible in the UI (developers can spot it).</description></item>
    /// </list>
    /// </remarks>
    public interface ILanguageService
    {
        /// <summary>
        /// Resolves the localized text for the given full key
        /// (<c>"{namespace}.{subKey}"</c> — split on the first <c>.</c>).
        /// Applies the fall-back chain documented on <see cref="ILanguageService"/>.
        /// </summary>
        /// <param name="lang">The BCP-47 language code (e.g. <c>"zh-TW"</c>).</param>
        /// <param name="fullKey">The full key, e.g. <c>"Common.OK"</c>, <c>"Customer.Field.Name.Caption"</c>.</param>
        /// <returns>The localized text, or the input <paramref name="fullKey"/> if all fall-backs miss.</returns>
        string GetLangText(string lang, string fullKey);

        /// <summary>
        /// Resolves the localized text using an explicit namespace and sub-key
        /// (skips the first-dot split that the full-key overload performs).
        /// Applies the fall-back chain documented on <see cref="ILanguageService"/>.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="namespace">The resource namespace (matches a file name stem; e.g. <c>"Common"</c>, <c>"Customer"</c>).</param>
        /// <param name="subKey">The sub-key within that namespace (e.g. <c>"OK"</c>, <c>"Field.Name.Caption"</c>).</param>
        /// <returns>The localized text, or <c>"{namespace}.{subKey}"</c> if all fall-backs miss.</returns>
        string GetLangText(string lang, string @namespace, string subKey);

        /// <summary>
        /// Attempts to resolve the localized text in the requested <paramref name="lang"/> only.
        /// **Does not apply the default-lang fall-back** — call sites that need the
        /// fall-back should use <see cref="GetLangText(string, string)"/> instead.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="fullKey">The full key, e.g. <c>"Common.OK"</c>.</param>
        /// <param name="text">The resolved text on hit; empty string on miss.</param>
        /// <returns><c>true</c> on hit; <c>false</c> on miss.</returns>
        bool TryGetLangText(string lang, string fullKey, out string text);

        /// <summary>
        /// Attempts to resolve the localized text in the requested <paramref name="lang"/> only,
        /// using an explicit namespace and sub-key. **Does not apply the default-lang fall-back.**
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="namespace">The resource namespace.</param>
        /// <param name="subKey">The sub-key within that namespace.</param>
        /// <param name="text">The resolved text on hit; empty string on miss.</param>
        /// <returns><c>true</c> on hit; <c>false</c> on miss.</returns>
        bool TryGetLangText(string lang, string @namespace, string subKey, out string text);

        /// <summary>
        /// Resolves a localized <see cref="LanguageEnum"/> (ordered code/text set) for the
        /// given full name (<c>"{namespace}.{enumName}"</c>). Applies the default-lang
        /// fall-back when the requested language has no matching enum.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="fullName">The full enum name, e.g. <c>"Common.Gender"</c>, <c>"Order.OrderStatus"</c>.</param>
        /// <returns>The matching <see cref="LanguageEnum"/>, or <c>null</c> if not found after fall-back.</returns>
        LanguageEnum? GetLangEnum(string lang, string fullName);

        /// <summary>
        /// Resolves a localized <see cref="LanguageEnum"/> using an explicit namespace
        /// and enum name. Applies the default-lang fall-back when the requested language
        /// has no matching enum.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="namespace">The resource namespace.</param>
        /// <param name="enumName">The enum name within that namespace.</param>
        /// <returns>The matching <see cref="LanguageEnum"/>, or <c>null</c> if not found after fall-back.</returns>
        LanguageEnum? GetLangEnum(string lang, string @namespace, string enumName);

        /// <summary>
        /// Convenience: resolves a single localized text for a code within a
        /// <see cref="LanguageEnum"/>. Applies the default-lang fall-back.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="fullName">The full enum name, e.g. <c>"Common.Gender"</c>.</param>
        /// <param name="code">The code to look up within the enum.</param>
        /// <returns>The localized text on hit; <c>null</c> when the enum or code is missing after fall-back.</returns>
        string? GetLangEnumText(string lang, string fullName, string code);
    }
}
