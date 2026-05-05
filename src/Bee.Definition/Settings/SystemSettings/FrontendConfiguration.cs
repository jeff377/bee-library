using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Frontend parameters and environment settings.
    /// </summary>
    [Description("Frontend parameters and environment settings.")]
    [TreeNode("Frontend")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class FrontendConfiguration
    {
        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
