using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Language
{
    /// <summary>
    /// A keyed collection of <see cref="LanguageItem"/> instances within a
    /// <see cref="LanguageResource"/>. Lookup by sub-key is O(1).
    /// </summary>
    [Description("Language item collection.")]
    [TreeNode("Items", true)]
    public class LanguageItemCollection : KeyCollectionBase<LanguageItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LanguageItemCollection"/>.
        /// </summary>
        public LanguageItemCollection() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LanguageItemCollection"/> with an owning resource.
        /// </summary>
        /// <param name="resource">The owning language resource.</param>
        public LanguageItemCollection(LanguageResource resource) : base(resource) { }

    }

    /// <summary>
    /// Convenience extension methods for <see cref="LanguageItemCollection"/>.
    /// </summary>
    public static class LanguageItemCollectionExtensions
    {
        /// <summary>
        /// Adds a localized text item.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="key">The sub-key.</param>
        /// <param name="value">The localized text.</param>
        public static LanguageItem Add(this LanguageItemCollection? collection, string key, string value)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var item = new LanguageItem { Key = key, Value = value };
            collection.Add(item);
            return item;
        }
    }
}
