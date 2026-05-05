using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A collection of layout fields within a <see cref="LayoutSection"/>.
    /// </summary>
    [Description("Layout field collection.")]
    [TreeNode("Fields", false)]
    public class LayoutFieldCollection : CollectionBase<LayoutField>
    {
    }
}
