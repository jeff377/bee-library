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

    }

    /// <summary>
    /// Convenience extension methods for <see cref="LanguageEnumEntryCollection"/>.
    /// </summary>
    public static class LanguageEnumEntryCollectionExtensions
    {
        /// <summary>
        /// Adds an entry to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="code">The persisted code.</param>
        /// <param name="text">The localized display text.</param>
        public static LanguageEnumEntry Add(this LanguageEnumEntryCollection? collection, string code, string text)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var entry = new LanguageEnumEntry { Code = code, Text = text };
            collection.Add(entry);
            return entry;
        }
    }
}
