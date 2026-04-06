using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Define.Layouts
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
