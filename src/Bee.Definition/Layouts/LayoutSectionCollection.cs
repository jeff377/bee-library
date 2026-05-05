using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A collection of layout sections.
    /// </summary>
    [Description("Layout section collection.")]
    [TreeNode("Sections", false)]
    public class LayoutSectionCollection : CollectionBase<LayoutSection>
    {
    }
}
