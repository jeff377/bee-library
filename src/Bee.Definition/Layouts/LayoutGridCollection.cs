using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A collection of layout grids (detail tables under a <see cref="FormLayout"/>).
    /// </summary>
    [Description("Layout grid collection.")]
    [TreeNode("Details", false)]
    public class LayoutGridCollection : CollectionBase<LayoutGrid>
    {
    }
}
