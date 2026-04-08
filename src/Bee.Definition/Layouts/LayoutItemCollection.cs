using System;
using System.ComponentModel;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A collection of layout items.
    /// </summary>
    [Serializable]
    [Description("Layout item collection.")]
    [TreeNode("Items", false)]
    public class LayoutItemCollection : CollectionBase<LayoutItemBase>
    {

    }
}
