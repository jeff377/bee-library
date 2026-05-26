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

        /// <summary>
        /// Adds a localized text item.
        /// </summary>
        /// <param name="key">The sub-key.</param>
        /// <param name="value">The localized text.</param>
        public LanguageItem Add(string key, string value)
        {
            var item = new LanguageItem { Key = key, Value = value };
            base.Add(item);
            return item;
        }
    }
}
