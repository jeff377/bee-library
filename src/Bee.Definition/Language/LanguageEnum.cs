using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Language
{
    /// <summary>
    /// An ordered, code-keyed set of localized entries — used for dropdown options,
    /// lookup tables, and any UI surface that needs both a stable identifier (code)
    /// and a localized label (text).
    /// </summary>
    /// <remarks>
    /// Entries preserve XML document order so callers can drive ComboBox /
    /// DataGrid lookup binding without an explicit sort step.
    /// </remarks>
    [Description("Ordered set of code/text pairs for a single enum / dropdown.")]
    public class LanguageEnum : KeyCollectionItem
    {
        private LanguageEnumEntryCollection? _entries;

        /// <summary>
        /// Gets or sets the enum name (unique within the parent <see cref="LanguageResource"/>).
        /// Proxies <see cref="KeyCollectionItem.Key"/> so the parent collection's keyed
        /// lookup (<c>enums["Gender"]</c>) uses the same identifier as the domain term.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Enum name, unique within the parent resource.")]
        public string Name
        {
            get => this.Key;
            set => this.Key = value;
        }

        /// <summary>
        /// Gets the ordered list of code/text entries.
        /// </summary>
        [XmlElement("Entry")]
        [Description("Ordered list of code/text entries.")]
        public LanguageEnumEntryCollection Entries
        {
            get => _entries ??= new LanguageEnumEntryCollection(this);
        }

        /// <summary>
        /// Looks up the localized text for the given code, or <c>null</c> if not present.
        /// </summary>
        /// <param name="code">The code to look up.</param>
        public string? GetText(string code)
        {
            return Entries.Contains(code) ? Entries[code].Text : null;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() => $"{Name} ({Entries.Count} entries)";
    }
}
