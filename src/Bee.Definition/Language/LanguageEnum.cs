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
        /// Gets or sets the ordered list of code/text entries.
        /// </summary>
        /// <remarks>
        /// WARNING: The setter exists for the mobile heads and must not be removed. A collection
        /// mapped as a repeated <c>[XmlElement]</c> is <b>assigned</b> by the reflection-only
        /// <c>XmlSerializer</c> that iOS uses, not filled through <c>Add</c> — without a setter it
        /// throws <c>ArgumentException: Property set method not found</c> mid-document, which
        /// surfaces as the misleading "error in XML document (line, col)". The desktop's
        /// Emit-based path fills get-only collections happily, so this only ever fails on device.
        /// <c>[XmlArray]</c> collections (<see cref="LanguageResource.Items"/>, <c>.Enums</c>) are not
        /// affected and stay get-only.
        /// <para>
        /// NOTE: The setter copies into the existing collection instead of replacing the field.
        /// <c>Owner</c> is get-only and can only be set by the constructor, so
        /// assigning the incoming instance — which <c>XmlSerializer</c> built through the
        /// parameterless constructor and therefore has no owner — would permanently break the
        /// back-reference to this enum.
        /// </para>
        /// </remarks>
        [XmlElement("Entry")]
        [Description("Ordered list of code/text entries.")]
        public LanguageEnumEntryCollection Entries
        {
            get => _entries ??= new LanguageEnumEntryCollection(this);
            set
            {
                var target = _entries ??= new LanguageEnumEntryCollection(this);
                if (ReferenceEquals(target, value))
                    return;

                target.Clear();
                if (value == null)
                    return;

                foreach (var entry in value)
                    target.Add(entry);
            }
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
