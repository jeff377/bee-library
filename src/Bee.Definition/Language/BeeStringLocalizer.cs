using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Bee.Definition.Language
{
    /// <summary>
    /// <see cref="IStringLocalizer{T}"/> adapter over <see cref="ILanguageService"/>.
    /// Lets Blazor / ASP.NET Core components (<c>@inject IStringLocalizer&lt;MyPage&gt;</c>)
    /// consume Bee.NET language resources through the standard .NET localisation
    /// surface without adopting <c>.resx</c> or the BCL <c>ResourceManager</c>.
    /// </summary>
    /// <remarks>
    /// The resource namespace is derived from <c>typeof(T).Name</c>. For example,
    /// <c>BeeStringLocalizer&lt;CustomerPage&gt;</c> resolves keys against
    /// <c>{LanguagePath}/{lang}/CustomerPage.Language.xml</c>. Place <c>typeof(T)</c>
    /// in a class whose name matches a namespace (e.g. <c>Common</c>, <c>Sys</c>).
    ///
    /// The current language defaults to <see cref="CultureInfo.CurrentUICulture"/>
    /// — callers wanting an explicit source (HTTP request, session lookup) should use the
    /// <see cref="BeeStringLocalizer{T}(ILanguageService, Func{string})"/> overload.
    /// </remarks>
    /// <typeparam name="T">A marker type whose simple name matches the resource namespace.</typeparam>
    public sealed class BeeStringLocalizer<T> : IStringLocalizer<T>
    {
        private readonly ILanguageService _service;
        private readonly Func<string> _langProvider;
        private readonly string _namespace;

        /// <summary>
        /// Initializes a new <see cref="BeeStringLocalizer{T}"/> using
        /// <see cref="CultureInfo.CurrentUICulture"/> as the language source. Suitable
        /// for ASP.NET Core's standard request-localization middleware.
        /// </summary>
        /// <param name="service">The underlying language resource service.</param>
        public BeeStringLocalizer(ILanguageService service)
            : this(service, static () => CultureInfo.CurrentUICulture.Name)
        { }

        /// <summary>
        /// Initializes a new <see cref="BeeStringLocalizer{T}"/> with an explicit
        /// language provider — typically wired to <c>SessionInfo.Culture</c> or another
        /// per-request value.
        /// </summary>
        /// <param name="service">The underlying language resource service.</param>
        /// <param name="langProvider">A delegate returning the BCP-47 language code for the current call.</param>
        public BeeStringLocalizer(ILanguageService service, Func<string> langProvider)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _langProvider = langProvider ?? throw new ArgumentNullException(nameof(langProvider));
            _namespace = typeof(T).Name;
        }

        /// <inheritdoc/>
        public LocalizedString this[string name]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(name);
                string lang = _langProvider() ?? string.Empty;
                bool hit = _service.TryGetLangText(lang, _namespace, name, out string text);
                if (!hit)
                {
                    // Fall through to the service-level default-lang fall-back, then return
                    // the full key as the LocalizedString value with ResourceNotFound=true
                    // so consumers can detect missing translations via the standard surface.
                    string fallback = _service.GetLangText(lang, _namespace, name);
                    bool resourceNotFound = string.Equals(fallback, $"{_namespace}.{name}", StringComparison.Ordinal);
                    return new LocalizedString(name, fallback, resourceNotFound);
                }
                return new LocalizedString(name, text, resourceNotFound: false);
            }
        }

        /// <inheritdoc/>
        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var localised = this[name];
                string formatted = string.Format(CultureInfo.CurrentCulture, localised.Value, arguments);
                return new LocalizedString(name, formatted, localised.ResourceNotFound, localised.SearchedLocation);
            }
        }

        /// <summary>
        /// Returns an empty enumeration. Bee.NET language resources are loaded on demand
        /// per key — there is no efficient "list all" surface and enumerating an entire
        /// namespace is not a supported use case.
        /// </summary>
        /// <param name="includeParentCultures">Unused.</param>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
    }
}
