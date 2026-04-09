using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A collection of layout groups.
    /// </summary>
    [Serializable]
    [Description("Layout group collection.")]
    [TreeNode("Groups", false)]
    public class LayoutGroupCollection : CollectionBase<LayoutGroup>
    {

    }
}
