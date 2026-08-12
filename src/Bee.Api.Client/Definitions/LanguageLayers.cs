using Bee.Definition.Language;

namespace Bee.Api.Client.Definitions
{
    /// <summary>
    /// The two layers fetched for one <c>(lang, namespace)</c> pair. Either may be absent.
    /// </summary>
    /// <param name="Base">The base-layer resource, or <c>null</c> when the namespace has no base file.</param>
    /// <param name="Customize">The tenant customization resource, or <c>null</c> when there is no override.</param>
    public readonly record struct LanguageLayers(LanguageResource? Base, LanguageResource? Customize);
}
