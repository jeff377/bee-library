using System;
using System.ComponentModel;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

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
