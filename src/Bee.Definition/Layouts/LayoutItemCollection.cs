using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A collection of layout items.
    /// </summary>
    [Description("Layout item collection.")]
    [TreeNode("Items", false)]
    public class LayoutItemCollection : CollectionBase<LayoutItemBase>
    {

    }
}
