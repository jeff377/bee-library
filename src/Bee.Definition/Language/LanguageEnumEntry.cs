using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Language
{
    /// <summary>
    /// A single entry in a <see cref="LanguageEnum"/> — a code/text pair for a
    /// dropdown option, lookup row, or other ordered enumeration of localized choices.
    /// </summary>
    [Description("Single code/text pair within a language enum.")]
    public class LanguageEnumEntry : KeyCollectionItem
    {
        /// <summary>
        /// Gets or sets the persisted code stored in the database.
        /// Proxies <see cref="KeyCollectionItem.Key"/> so the collection's keyed lookup
        /// (<c>entries["M"]</c>) uses the same identifier as the domain term.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Persisted code stored in the database.")]
        public string Code
        {
            get => this.Key;
            set => this.Key = value;
        }

        /// <summary>
        /// Gets or sets the localized display text.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Localized display text.")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() => $"{Code} = {Text}";
    }
}
