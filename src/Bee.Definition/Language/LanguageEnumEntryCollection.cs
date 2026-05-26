using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Language
{
    /// <summary>
    /// A keyed collection of <see cref="LanguageEnumEntry"/> instances within a
    /// <see cref="LanguageEnum"/>. Insertion order is preserved (driving UI ComboBox
    /// / lookup binding); lookup by code is O(1).
    /// </summary>
    [Description("Language enum entry collection.")]
    [TreeNode("Entries", true)]
    public class LanguageEnumEntryCollection : KeyCollectionBase<LanguageEnumEntry>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LanguageEnumEntryCollection"/>.
        /// </summary>
        public LanguageEnumEntryCollection() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LanguageEnumEntryCollection"/> with an owning enum.
        /// </summary>
        /// <param name="languageEnum">The owning language enum.</param>
        public LanguageEnumEntryCollection(LanguageEnum languageEnum) : base(languageEnum) { }

        /// <summary>
        /// Adds an entry to the collection.
        /// </summary>
        /// <param name="code">The persisted code.</param>
        /// <param name="text">The localized display text.</param>
        public LanguageEnumEntry Add(string code, string text)
        {
            var entry = new LanguageEnumEntry { Code = code, Text = text };
            base.Add(entry);
            return entry;
        }
    }
}
