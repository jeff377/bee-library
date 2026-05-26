using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Definition.Language
{
    /// <summary>
    /// A single language resource file — one namespace × one language.
    /// Holds the localized text items and enum entries that share this namespace
    /// (typically a ProgId, a module prefix such as <c>"Sys"</c>, or the shared
    /// <c>"Common"</c> bucket).
    /// </summary>
    /// <remarks>
    /// Persistence is XML (under <c>{DefinePath}/Language/{lang}/{namespace}.Language.xml</c>);
    /// JSON serialization is supported for JSON-RPC delivery to JS / TypeScript front-ends.
    /// </remarks>
    [Description("Localized text and enum entries for one namespace × one language.")]
    [XmlRoot("LanguageResource")]
    public class LanguageResource
    {
        private LanguageItemCollection? _items;
        private LanguageEnumCollection? _enums;

        /// <summary>
        /// Gets or sets the namespace this resource belongs to.
        /// Mirrors the file name stem and equals the first segment of every key
        /// resolved against this resource (e.g. <c>"Customer"</c>, <c>"Common"</c>, <c>"Sys"</c>).
        /// </summary>
        [XmlAttribute]
        [Description("Namespace (matches file name stem; first segment of full keys).")]
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the BCP-47 language code (e.g. <c>"zh-TW"</c>, <c>"en-US"</c>).
        /// </summary>
        [XmlAttribute]
        [Description("BCP-47 language code.")]
        public string Lang { get; set; } = string.Empty;

        /// <summary>
        /// Gets the keyed collection of localized text items, looked up by sub-key.
        /// </summary>
        [XmlArray("Items")]
        [XmlArrayItem(typeof(LanguageItem))]
        [Description("Keyed collection of localized text items.")]
        public LanguageItemCollection Items
        {
            get => _items ??= new LanguageItemCollection(this);
        }

        /// <summary>
        /// Gets the keyed collection of localized enums (ordered dropdown / lookup sets).
        /// </summary>
        [XmlArray("Enums")]
        [XmlArrayItem(typeof(LanguageEnum))]
        [Description("Keyed collection of localized enums.")]
        public LanguageEnumCollection Enums
        {
            get => _enums ??= new LanguageEnumCollection(this);
        }

        /// <summary>
        /// Looks up the localized text for the given sub-key, or <c>null</c> if not present.
        /// </summary>
        /// <param name="subKey">The sub-key (everything after the leading namespace segment).</param>
        public string? GetText(string subKey)
        {
            return Items.Contains(subKey) ? Items[subKey].Value : null;
        }

        /// <summary>
        /// Looks up the localized enum for the given name, or <c>null</c> if not present.
        /// </summary>
        /// <param name="name">The enum name.</param>
        public LanguageEnum? GetEnum(string name)
        {
            return Enums.Contains(name) ? Enums[name] : null;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() =>
            $"{Namespace} [{Lang}] ({Items.Count} items, {Enums.Count} enums)";
    }
}
