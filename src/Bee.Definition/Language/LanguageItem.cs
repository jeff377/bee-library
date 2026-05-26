using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Language
{
    /// <summary>
    /// A single localized text entry within a <see cref="LanguageResource"/>.
    /// </summary>
    [Description("Single localized text entry.")]
    public class LanguageItem : KeyCollectionItem
    {
        /// <summary>
        /// Gets or sets the sub-key (within the parent namespace).
        /// Examples: <c>"OK"</c>, <c>"Field.Name.Caption"</c>.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Sub-key within the parent namespace.")]
        public override string Key
        {
            get => base.Key;
            set => base.Key = value;
        }

        /// <summary>
        /// Gets or sets the localized text.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Localized text.")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() => $"{Key} = {Value}";
    }
}
