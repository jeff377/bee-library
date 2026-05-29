using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.Definition.Storage
{
    /// <summary>
    /// Reads the tenant customization-override layer for the three customizable definition
    /// types (Language, ProgramSettings, FormLayout). Each accessor returns the pure
    /// customization content for the given customization code, or <c>null</c> when the tenant
    /// provides no override (no file) — it never falls back to nor merges with the base layer.
    /// </summary>
    /// <remarks>
    /// This interface is intentionally separate from <see cref="IDefineAccess"/>: base reads are
    /// unchanged and continue to flow through <see cref="IDefineAccess"/>. Consumers overlay the
    /// two layers at lookup granularity (per key / per progId / whole-file), never producing a
    /// merged object and never mutating the base cache.
    /// </remarks>
    public interface ICustomizeDefineReader
    {
        /// <summary>
        /// Gets the customization override of the language resource for the given customization
        /// code, language and namespace; <c>null</c> when no override exists.
        /// </summary>
        /// <param name="custCode">The tenant customization code.</param>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        LanguageResource? GetCustomizeLanguage(string custCode, string lang, string ns);

        /// <summary>
        /// Gets the customization override of the program settings for the given customization
        /// code; <c>null</c> when no override exists.
        /// </summary>
        /// <param name="custCode">The tenant customization code.</param>
        ProgramSettings? GetCustomizeProgramSettings(string custCode);

        /// <summary>
        /// Gets the customization override of the form layout for the given customization code
        /// and layout id; <c>null</c> when no override exists.
        /// </summary>
        /// <param name="custCode">The tenant customization code.</param>
        /// <param name="layoutId">The form layout ID.</param>
        FormLayout? GetCustomizeFormLayout(string custCode, string layoutId);
    }
}
